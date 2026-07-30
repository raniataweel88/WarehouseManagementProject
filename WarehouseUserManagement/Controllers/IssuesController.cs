using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WarehouseUserManagement.Authorization;
using WarehouseUserManagement.Data;
using WarehouseUserManagement.Models;
using WarehouseUserManagement.Models.ViewModels;
using WarehouseUserManagement.Services;

namespace WarehouseUserManagement.Controllers
{
    public class IssuesController : Controller
    {
        private readonly AppDbContext _context;
        private readonly ICurrentUserService _currentUser;

        public IssuesController(AppDbContext context, ICurrentUserService currentUser)
        {
            _context = context;
            _currentUser = currentUser;
        }

        // GET: /Issues?searchTerm=&departmentFilter=&statusFilter=&pageNumber=1
        [HasPermission("View Issues")]
        public async Task<IActionResult> Index(IssueSearchRequest request)
        {
            var query = _context.Issues.AsNoTracking().Include(i => i.Department).AsQueryable();

            if (!string.IsNullOrWhiteSpace(request.SearchTerm))
            {
                var term = request.SearchTerm.Trim();
                query = query.Where(i => i.IssueNumber.StartsWith(term) || i.ReceiverName.StartsWith(term));
            }

            if (request.DepartmentFilter.HasValue)
                query = query.Where(i => i.DepartmentId == request.DepartmentFilter.Value);

            if (request.StatusFilter.HasValue)
                query = query.Where(i => i.Status == request.StatusFilter.Value);

            var totalCount = await query.CountAsync();

            var pageSize = request.PageSize is > 0 and <= 100 ? request.PageSize : 10;
            var pageNumber = request.PageNumber < 1 ? 1 : request.PageNumber;

            var issues = await query
                .OrderByDescending(i => i.IssueDate).ThenByDescending(i => i.IssueId)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(i => new IssueRow
                {
                    IssueId = i.IssueId,
                    IssueNumber = i.IssueNumber,
                    IssueDate = i.IssueDate,
                    DepartmentName = i.Department!.DepartmentName,
                    ReceiverName = i.ReceiverName,
                    Status = i.Status
                })
                .ToListAsync();

            var vm = new IssueListViewModel
            {
                Issues = issues,
                SearchTerm = request.SearchTerm,
                DepartmentFilter = request.DepartmentFilter,
                StatusFilter = request.StatusFilter,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalCount = totalCount,
                DepartmentOptions = await GetActiveDepartmentOptionsAsync()
            };

            return View(vm);
        }

        // GET: /Issues/Create
        [HasPermission("Create Issue")]
        public async Task<IActionResult> Create()
        {
            var vm = new IssueFormViewModel
            {
                IssueNumber = await GenerateNextIssueNumberAsync(),
                DepartmentOptions = await GetActiveDepartmentOptionsAsync(),
                ProductOptions = await GetActiveProductOptionsAsync(),
                Lines = new List<IssueLineViewModel> { new() }
            };
            return View(vm);
        }

        // POST: /Issues/Create  (يُنشئ السند بحالة Draft، لا يؤثر على المخزون بعد)
        [HttpPost]
        [ValidateAntiForgeryToken]
        [HasPermission("Create Issue")]
        public async Task<IActionResult> Create(IssueFormViewModel model)
        {
            await ValidateIssueAsync(model, issueIdToExclude: null);

            if (!ModelState.IsValid)
            {
                model.DepartmentOptions = await GetActiveDepartmentOptionsAsync();
                model.ProductOptions = await GetActiveProductOptionsAsync();
                return View(model);
            }

            var issue = BuildIssueFromModel(model);
            issue.Status = IssueStatus.Draft;
            issue.CreatedBy = _currentUser.Username;
            issue.CreatedDate = DateTime.UtcNow;

            _context.Issues.Add(issue);
            await _context.SaveChangesAsync();

            TempData["Success"] = "تم حفظ سند الصرف كمسودة";
            return RedirectToAction(nameof(Index));
        }

        // GET: /Issues/Edit/5  (يعمل فقط للسندات بحالة Draft)
        [HasPermission("Edit Issue")]
        public async Task<IActionResult> Edit(int id)
        {
            var issue = await _context.Issues.AsNoTracking()
                .Include(i => i.Details)
                .FirstOrDefaultAsync(i => i.IssueId == id);
            if (issue is null) return NotFound();

            if (issue.Status != IssueStatus.Draft)
            {
                TempData["Error"] = "لا يمكن تعديل سند معتمد أو ملغى";
                return RedirectToAction(nameof(Index));
            }

            var vm = new IssueFormViewModel
            {
                IssueId = issue.IssueId,
                IssueNumber = issue.IssueNumber,
                IssueDate = issue.IssueDate,
                DepartmentId = issue.DepartmentId,
                ReceiverName = issue.ReceiverName,
                Notes = issue.Notes,
                RowVersion = issue.RowVersion,
                DepartmentOptions = await GetActiveDepartmentOptionsAsync(),
                ProductOptions = await GetActiveProductOptionsAsync(),
                Lines = issue.Details.Select(d => new IssueLineViewModel
                {
                    ProductId = d.ProductId,
                    Quantity = d.Quantity
                }).ToList()
            };

            return View(vm);
        }

        // POST: /Issues/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [HasPermission("Edit Issue")]
        public async Task<IActionResult> Edit(int id, IssueFormViewModel model)
        {
            if (id != model.IssueId) return NotFound();

            var issue = await _context.Issues
                .Include(i => i.Details)
                .FirstOrDefaultAsync(i => i.IssueId == id);
            if (issue is null) return NotFound();

            if (issue.Status != IssueStatus.Draft)
            {
                TempData["Error"] = "لا يمكن تعديل سند معتمد أو ملغى";
                return RedirectToAction(nameof(Index));
            }

            await ValidateIssueAsync(model, issueIdToExclude: id);

            if (!ModelState.IsValid)
            {
                model.DepartmentOptions = await GetActiveDepartmentOptionsAsync();
                model.ProductOptions = await GetActiveProductOptionsAsync();
                return View(model);
            }

            if (model.RowVersion is not null)
                _context.Entry(issue).Property(i => i.RowVersion).OriginalValue = model.RowVersion;

            issue.IssueNumber = model.IssueNumber;
            issue.IssueDate = model.IssueDate;
            issue.DepartmentId = model.DepartmentId;
            issue.ReceiverName = model.ReceiverName;
            issue.Notes = model.Notes;

            // استبدال كل سطور السند بالسطور الجديدة (السند ما زال Draft، لا تأثير على المخزون بعد)
            _context.IssueDetails.RemoveRange(issue.Details);
            issue.Details = BuildDetailsFromModel(model);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                ModelState.AddModelError(string.Empty, "تم تعديل هذا السند من جلسة أخرى، الرجاء إعادة تحميل الصفحة");
                model.DepartmentOptions = await GetActiveDepartmentOptionsAsync();
                model.ProductOptions = await GetActiveProductOptionsAsync();
                return View(model);
            }

            TempData["Success"] = "تم تعديل سند الصرف بنجاح";
            return RedirectToAction(nameof(Index));
        }

        // GET: /Issues/Details/5
        [HasPermission("View Issues")]
        public async Task<IActionResult> Details(int id)
        {
            var issue = await _context.Issues.AsNoTracking()
                .Include(i => i.Department)
                .Include(i => i.Details).ThenInclude(d => d.Product)
                .FirstOrDefaultAsync(i => i.IssueId == id);
            if (issue is null) return NotFound();

            var vm = new IssueDetailsViewModel
            {
                IssueId = issue.IssueId,
                IssueNumber = issue.IssueNumber,
                IssueDate = issue.IssueDate,
                DepartmentName = issue.Department!.DepartmentName,
                ReceiverName = issue.ReceiverName,
                Notes = issue.Notes,
                Status = issue.Status,
                CreatedBy = issue.CreatedBy,
                CreatedDate = issue.CreatedDate,
                PostedBy = issue.PostedBy,
                PostedDate = issue.PostedDate,
                CancelledBy = issue.CancelledBy,
                CancelledDate = issue.CancelledDate,
                Lines = issue.Details.Select(d => new IssueDetailRow
                {
                    ProductName = d.Product!.ProductName,
                    Unit = d.Product.Unit,
                    Quantity = d.Quantity
                }).ToList()
            };

            return View(vm);
        }

        // POST: /Issues/Approve/5  (Draft -> Posted: يخصم الكميات من المخزون وينشئ حركة مخزون)
        [HttpPost]
        [ValidateAntiForgeryToken]
        [HasPermission("Post Issue")]
        public async Task<IActionResult> Approve(int id)
        {
            var issue = await _context.Issues
                .Include(i => i.Details).ThenInclude(d => d.Product)
                .FirstOrDefaultAsync(i => i.IssueId == id);
            if (issue is null) return NotFound();

            if (issue.Status != IssueStatus.Draft)
            {
                TempData["Error"] = "لا يمكن اعتماد سند إلا وهو بحالة مسودة";
                return RedirectToAction(nameof(Index));
            }

            if (!issue.Details.Any())
            {
                TempData["Error"] = "لا يمكن اعتماد سند لا يحتوي على أصناف";
                return RedirectToAction(nameof(Index));
            }

            // فحص أخير للكميات المتوفرة وقت الاعتماد (قد يكون المخزون قد تغيّر منذ إنشاء المسودة)
            var insufficient = issue.Details
                .Where(line => line.Quantity > line.Product!.CurrentQuantity)
                .Select(line => $"{line.Product!.ProductName} (المتوفر: {line.Product.CurrentQuantity}, المطلوب: {line.Quantity})")
                .ToList();

            if (insufficient.Any())
            {
                TempData["Error"] = "الكمية المطلوبة أكبر من الكمية المتوفرة للأصناف التالية: " + string.Join("، ", insufficient);
                return RedirectToAction(nameof(Index));
            }

            foreach (var line in issue.Details)
            {
                var product = line.Product!;

                product.CurrentQuantity -= line.Quantity;
                product.UpdatedBy = _currentUser.Username;
                product.UpdatedDate = DateTime.UtcNow;

                _context.StockTransactions.Add(new StockTransaction
                {
                    ProductId = product.ProductId,
                    TransactionType = StockTransactionType.Issue,
                    Quantity = -line.Quantity,
                    UnitCost = product.AverageCost,
                    ReferenceType = "Issue",
                    ReferenceId = issue.IssueId,
                    TransactionDate = DateTime.UtcNow,
                    CreatedBy = _currentUser.Username
                });
            }

            issue.Status = IssueStatus.Posted;
            issue.PostedBy = _currentUser.Username;
            issue.PostedDate = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            TempData["Success"] = "تم اعتماد سند الصرف وتحديث المخزون";
            return RedirectToAction(nameof(Index));
        }

        // POST: /Issues/Cancel/5
        // Draft -> Cancelled: إلغاء بسيط بدون أي أثر على المخزون (لم يُصرف بعد)
        // Posted -> Cancelled: إعادة الكميات المصروفة سابقاً وإنشاء حركة مخزون عكسية
        [HttpPost]
        [ValidateAntiForgeryToken]
        [HasPermission("Cancel Issue")]
        public async Task<IActionResult> Cancel(int id)
        {
            var issue = await _context.Issues
                .Include(i => i.Details).ThenInclude(d => d.Product)
                .FirstOrDefaultAsync(i => i.IssueId == id);
            if (issue is null) return NotFound();

            if (issue.Status == IssueStatus.Cancelled)
            {
                TempData["Error"] = "السند ملغى مسبقاً";
                return RedirectToAction(nameof(Index));
            }

            if (issue.Status == IssueStatus.Posted)
            {
                foreach (var line in issue.Details)
                {
                    var product = line.Product!;

                    product.CurrentQuantity += line.Quantity;
                    product.UpdatedBy = _currentUser.Username;
                    product.UpdatedDate = DateTime.UtcNow;

                    _context.StockTransactions.Add(new StockTransaction
                    {
                        ProductId = product.ProductId,
                        TransactionType = StockTransactionType.IssueReverse,
                        Quantity = line.Quantity,
                        UnitCost = product.AverageCost,
                        ReferenceType = "Issue",
                        ReferenceId = issue.IssueId,
                        TransactionDate = DateTime.UtcNow,
                        CreatedBy = _currentUser.Username
                    });
                }
            }

            issue.Status = IssueStatus.Cancelled;
            issue.CancelledBy = _currentUser.Username;
            issue.CancelledDate = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            TempData["Success"] = "تم إلغاء سند الصرف";
            return RedirectToAction(nameof(Index));
        }

        // ===== Helpers =====

        private async Task ValidateIssueAsync(IssueFormViewModel model, int? issueIdToExclude)
        {
            var numberExists = await _context.Issues.AnyAsync(i =>
                i.IssueNumber == model.IssueNumber &&
                (issueIdToExclude == null || i.IssueId != issueIdToExclude));
            if (numberExists)
                ModelState.AddModelError(nameof(model.IssueNumber), "رقم الصرف مستخدم مسبقاً");

            var departmentActive = await _context.Departments.AnyAsync(d => d.DepartmentId == model.DepartmentId && d.IsActive);
            if (!departmentActive)
                ModelState.AddModelError(nameof(model.DepartmentId), "القسم المختار غير موجود أو موقوف");

            var validLines = model.Lines.Where(l => l.ProductId > 0).ToList();

            if (!validLines.Any())
                ModelState.AddModelError(string.Empty, "يجب إضافة صنف واحد على الأقل للسند");

            var duplicateProducts = validLines.GroupBy(l => l.ProductId).Where(g => g.Count() > 1);
            if (duplicateProducts.Any())
                ModelState.AddModelError(string.Empty, "لا يسمح بتكرار نفس الصنف أكثر من مرة في نفس السند");

            if (validLines.Any())
            {
                var productIds = validLines.Select(l => l.ProductId).Distinct().ToList();
                var activeProducts = await _context.Products
                    .Where(p => productIds.Contains(p.ProductId) && p.IsActive)
                    .ToDictionaryAsync(p => p.ProductId, p => p.CurrentQuantity);

                if (activeProducts.Count != productIds.Count)
                    ModelState.AddModelError(string.Empty, "أحد الأصناف المختارة غير موجود أو موقوف");

                // تحذير مبكر إن كانت الكمية المطلوبة أكبر من المتوفر حالياً (يُعاد التحقق إلزامياً وقت الاعتماد)
                foreach (var line in validLines)
                {
                    if (activeProducts.TryGetValue(line.ProductId, out var available) && line.Quantity > available)
                        ModelState.AddModelError(string.Empty, $"الكمية المطلوبة أكبر من الكمية المتوفرة (المتوفر حالياً: {available})");
                }
            }
        }

        private Issue BuildIssueFromModel(IssueFormViewModel model)
        {
            return new Issue
            {
                IssueNumber = model.IssueNumber,
                IssueDate = model.IssueDate,
                DepartmentId = model.DepartmentId,
                ReceiverName = model.ReceiverName,
                Notes = model.Notes,
                Details = BuildDetailsFromModel(model)
            };
        }

        private static List<IssueDetail> BuildDetailsFromModel(IssueFormViewModel model)
        {
            return model.Lines
                .Where(l => l.ProductId > 0)
                .Select(l => new IssueDetail
                {
                    ProductId = l.ProductId,
                    Quantity = l.Quantity
                })
                .ToList();
        }

        private async Task<List<IssueProductOption>> GetActiveProductOptionsAsync()
        {
            return await _context.Products.AsNoTracking()
                .Where(p => p.IsActive)
                .OrderBy(p => p.ProductName)
                .Select(p => new IssueProductOption
                {
                    ProductId = p.ProductId,
                    ProductName = p.ProductName,
                    CategoryName = p.Category!.CategoryName,
                    Unit = p.Unit,
                    CurrentQuantity = p.CurrentQuantity
                })
                .ToListAsync();
        }

        private async Task<List<DepartmentOption>> GetActiveDepartmentOptionsAsync()
        {
            return await _context.Departments.AsNoTracking()
                .Where(d => d.IsActive)
                .OrderBy(d => d.DepartmentName)
                .Select(d => new DepartmentOption { DepartmentId = d.DepartmentId, DepartmentName = d.DepartmentName })
                .ToListAsync();
        }

        // يولّد رقم صرف افتراضي متسلسل (1, 2, 3, ...) يزيد تلقائياً في كل مرة يُفتح فيها نموذج سند جديد،
        // مع بقاء الحقل قابلاً للتعديل يدوياً إذا رغب المستخدم.
        private async Task<string> GenerateNextIssueNumberAsync()
        {
            var lastNumbers = await _context.Issues
                .OrderByDescending(i => i.IssueId)
                .Select(i => i.IssueNumber)
                .Take(20)
                .ToListAsync();

            var nextSeq = 1;
            foreach (var number in lastNumbers)
            {
                var digits = new string((number ?? string.Empty).Where(char.IsDigit).ToArray());
                if (int.TryParse(digits, out var parsed) && parsed >= nextSeq)
                {
                    nextSeq = parsed + 1;
                    break;
                }
            }

            return $"{nextSeq}";
        }
    }
}
