using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WarehouseUserManagement.Authorization;
using WarehouseUserManagement.Data;
using WarehouseUserManagement.Models;
using WarehouseUserManagement.Models.ViewModels;
using WarehouseUserManagement.Services;

namespace WarehouseUserManagement.Controllers
{
    public class StockTakeController : Controller
    {
        private readonly AppDbContext _context;
        private readonly ICurrentUserService _currentUser;

        public StockTakeController(AppDbContext context, ICurrentUserService currentUser)
        {
            _context = context;
            _currentUser = currentUser;
        }

        // GET: /StockTake?searchTerm=&statusFilter=&pageNumber=1
        [HasPermission("View StockTake")]
        public async Task<IActionResult> Index(StockTakeSearchRequest request)
        {
            var query = _context.StockTakes.AsNoTracking()
                .Include(s => s.Details).ThenInclude(d => d.Product).ThenInclude(p => p!.Category)
                .AsQueryable();

            // البحث الآن يتم على باركود الصنف داخل سطور السند (وليس على ملاحظات السند)
            if (!string.IsNullOrWhiteSpace(request.SearchTerm))
            {
                var term = request.SearchTerm.Trim();
                query = query.Where(s => s.Details.Any(d => d.Product != null && d.Product.Barcode != null && d.Product.Barcode.StartsWith(term)));
            }

            if (request.StatusFilter.HasValue)
                query = query.Where(s => s.Status == request.StatusFilter.Value);

            // فلترة حسب التصنيف: يظهر السند إذا كان يحتوي على صنف واحد على الأقل من هذا التصنيف
            if (request.CategoryFilter.HasValue)
                query = query.Where(s => s.Details.Any(d => d.Product != null && d.Product.CategoryId == request.CategoryFilter.Value));

            var totalCount = await query.CountAsync();

            var pageSize = request.PageSize is > 0 and <= 100 ? request.PageSize : 10;
            var pageNumber = request.PageNumber < 1 ? 1 : request.PageNumber;

            var stockTakes = await query
                .OrderByDescending(s => s.StockTakeDate).ThenByDescending(s => s.StockTakeId)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(s => new StockTakeRow
                {
                    StockTakeId = s.StockTakeId,
                    StockTakeDate = s.StockTakeDate,
                    Notes = s.Notes,
                    Status = s.Status,
                    LinesCount = s.Details.Count,
                    Categories = s.Details.Where(d => d.Product != null && d.Product.Category != null)
                        .Select(d => d.Product!.Category!.CategoryName).Distinct().ToList(),
                    Barcodes = s.Details.Where(d => d.Product != null && d.Product.Barcode != null)
                        .Select(d => d.Product!.Barcode!).Distinct().ToList()
                })
                .ToListAsync();

            var categories = await _context.Categories.AsNoTracking()
                .Where(c => !c.IsDeleted)
                .OrderBy(c => c.CategoryName)
                .Select(c => new CategoryOption { CategoryId = c.CategoryId, CategoryName = c.CategoryName })
                .ToListAsync();

            var vm = new StockTakeListViewModel
            {
                StockTakes = stockTakes,
                SearchTerm = request.SearchTerm,
                StatusFilter = request.StatusFilter,
                CategoryFilter = request.CategoryFilter,
                Categories = categories,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalCount = totalCount
            };

            return View(vm);
        }

        // GET: /StockTake/Create
        [HasPermission("Create StockTake")]
        public async Task<IActionResult> Create()
        {
            var vm = new StockTakeFormViewModel
            {
                ProductOptions = await GetActiveProductOptionsAsync(),
                Lines = new List<StockTakeLineViewModel> { new() }
            };
            return View(vm);
        }

        // POST: /StockTake/Create  (يُنشئ السند بحالة Draft، لا يؤثر على المخزون بعد)
        [HttpPost]
        [ValidateAntiForgeryToken]
        [HasPermission("Create StockTake")]
        public async Task<IActionResult> Create(StockTakeFormViewModel model)
        {
            var systemQuantities = await ValidateStockTakeAsync(model);

            if (!ModelState.IsValid)
            {
                model.ProductOptions = await GetActiveProductOptionsAsync();
                return View(model);
            }

            var stockTake = new StockTake
            {
                StockTakeDate = model.StockTakeDate,
                Notes = model.Notes,
                Status = StockTakeStatus.Draft,
                CreatedBy = _currentUser.Username,
                CreatedDate = DateTime.UtcNow,
                Details = BuildDetailsFromModel(model, systemQuantities)
            };

            _context.StockTakes.Add(stockTake);
            await _context.SaveChangesAsync();

            TempData["Success"] = "تم حفظ سند الجرد كمسودة";
            return RedirectToAction(nameof(Index));
        }

        // GET: /StockTake/Edit/5  (يعمل فقط للسندات بحالة Draft)
        [HasPermission("Edit StockTake")]
        public async Task<IActionResult> Edit(int id)
        {
            var stockTake = await _context.StockTakes.AsNoTracking()
                .Include(s => s.Details)
                .FirstOrDefaultAsync(s => s.StockTakeId == id);
            if (stockTake is null) return NotFound();

            if (stockTake.Status != StockTakeStatus.Draft)
            {
                TempData["Error"] = "لا يمكن تعديل سند جرد معتمد";
                return RedirectToAction(nameof(Index));
            }

            var vm = new StockTakeFormViewModel
            {
                StockTakeId = stockTake.StockTakeId,
                StockTakeDate = stockTake.StockTakeDate,
                Notes = stockTake.Notes,
                RowVersion = stockTake.RowVersion,
                ProductOptions = await GetActiveProductOptionsAsync(),
                Lines = stockTake.Details.Select(d => new StockTakeLineViewModel
                {
                    ProductId = d.ProductId,
                    ActualQuantity = d.ActualQuantity
                }).ToList()
            };

            return View(vm);
        }

        // POST: /StockTake/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [HasPermission("Edit StockTake")]
        public async Task<IActionResult> Edit(int id, StockTakeFormViewModel model)
        {
            if (id != model.StockTakeId) return NotFound();

            var stockTake = await _context.StockTakes
                .Include(s => s.Details)
                .FirstOrDefaultAsync(s => s.StockTakeId == id);
            if (stockTake is null) return NotFound();

            if (stockTake.Status != StockTakeStatus.Draft)
            {
                TempData["Error"] = "لا يمكن تعديل سند جرد معتمد";
                return RedirectToAction(nameof(Index));
            }

            var systemQuantities = await ValidateStockTakeAsync(model);

            if (!ModelState.IsValid)
            {
                model.ProductOptions = await GetActiveProductOptionsAsync();
                return View(model);
            }

            if (model.RowVersion is not null)
                _context.Entry(stockTake).Property(s => s.RowVersion).OriginalValue = model.RowVersion;

            stockTake.StockTakeDate = model.StockTakeDate;
            stockTake.Notes = model.Notes;

            // استبدال كل سطور السند بالسطور الجديدة (السند ما زال Draft، لا تأثير على المخزون بعد)
            _context.StockTakeDetails.RemoveRange(stockTake.Details);
            stockTake.Details = BuildDetailsFromModel(model, systemQuantities);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                ModelState.AddModelError(string.Empty, "تم تعديل هذا السند من جلسة أخرى، الرجاء إعادة تحميل الصفحة");
                model.ProductOptions = await GetActiveProductOptionsAsync();
                return View(model);
            }

            TempData["Success"] = "تم تعديل سند الجرد بنجاح";
            return RedirectToAction(nameof(Index));
        }

        // GET: /StockTake/Details/5
        [HasPermission("View StockTake")]
        public async Task<IActionResult> Details(int id)
        {
            var stockTake = await _context.StockTakes.AsNoTracking()
                .Include(s => s.Details).ThenInclude(d => d.Product)
                .FirstOrDefaultAsync(s => s.StockTakeId == id);
            if (stockTake is null) return NotFound();

            var vm = new StockTakeDetailsViewModel
            {
                StockTakeId = stockTake.StockTakeId,
                StockTakeDate = stockTake.StockTakeDate,
                Notes = stockTake.Notes,
                Status = stockTake.Status,
                CreatedBy = stockTake.CreatedBy,
                CreatedDate = stockTake.CreatedDate,
                PostedBy = stockTake.PostedBy,
                PostedDate = stockTake.PostedDate,
                Lines = stockTake.Details.Select(d => new StockTakeDetailRow
                {
                    ProductName = d.Product!.ProductName,
                    Unit = d.Product.Unit,
                    SystemQuantity = d.SystemQuantity,
                    ActualQuantity = d.ActualQuantity,
                    Difference = d.Difference
                }).ToList()
            };

            return View(vm);
        }

        // POST: /StockTake/Post/5
        // Draft -> Posted: تُعاد قراءة الكمية الحالية الفعلية لكل صنف وقت الاعتماد نفسه (وليس وقت إنشاء
        // المسودة)، لضمان صحة النتيجة حتى لو تحرّك المخزون بحركة أخرى (شراء/صرف) بين الاثنين. يُنشأ
        // Adjustment Transaction بفرق = (الفعلي - الكمية الحالية وقت الاعتماد)، وتُصبح CurrentQuantity
        // للصنف مساوية تماماً للكمية الفعلية المعدودة.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [HasPermission("Post StockTake")]
        public async Task<IActionResult> Post(int id)
        {
            var stockTake = await _context.StockTakes
                .Include(s => s.Details).ThenInclude(d => d.Product)
                .FirstOrDefaultAsync(s => s.StockTakeId == id);
            if (stockTake is null) return NotFound();

            if (stockTake.Status != StockTakeStatus.Draft)
            {
                TempData["Error"] = "لا يمكن اعتماد سند إلا وهو بحالة مسودة";
                return RedirectToAction(nameof(Index));
            }

            if (!stockTake.Details.Any())
            {
                TempData["Error"] = "لا يمكن اعتماد سند جرد لا يحتوي على أصناف";
                return RedirectToAction(nameof(Index));
            }

            foreach (var line in stockTake.Details)
            {
                var product = line.Product!;

                // إعادة الحساب على أساس الكمية الحية وقت الاعتماد (وليس القيمة المخزَّنة وقت الإنشاء)
                var liveSystemQuantity = product.CurrentQuantity;
                var appliedDifference = line.ActualQuantity - liveSystemQuantity;

                // تحديث سطر السند ليعكس القيم الفعلية المطبَّقة وقت الاعتماد (Audit Trail دقيق)
                line.SystemQuantity = liveSystemQuantity;
                line.Difference = appliedDifference;

                if (appliedDifference != 0)
                {
                    product.CurrentQuantity = line.ActualQuantity;
                    product.UpdatedBy = _currentUser.Username;
                    product.UpdatedDate = DateTime.UtcNow;

                    _context.StockTransactions.Add(new StockTransaction
                    {
                        ProductId = product.ProductId,
                        TransactionType = StockTransactionType.Adjustment,
                        Quantity = appliedDifference,
                        UnitCost = product.AverageCost,
                        ReferenceType = "StockTake",
                        ReferenceId = stockTake.StockTakeId,
                        TransactionDate = DateTime.UtcNow,
                        CreatedBy = _currentUser.Username
                    });
                }
            }

            stockTake.Status = StockTakeStatus.Posted;
            stockTake.PostedBy = _currentUser.Username;
            stockTake.PostedDate = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            TempData["Success"] = "تم اعتماد سند الجرد وتحديث المخزون حسب الفروقات";
            return RedirectToAction(nameof(Index));
        }

        // ===== Helpers =====

        /// <summary>
        /// يتحقق من صحة السند ويرجع خريطة (ProductId -> الكمية الحالية بالنظام وقت الحفظ)، لتُستخدم
        /// لاحقاً في بناء السطور. SystemQuantity لا تُؤخذ أبداً من الـ ViewModel القادم من المتصفح.
        /// </summary>
        private async Task<Dictionary<int, decimal>> ValidateStockTakeAsync(StockTakeFormViewModel model)
        {
            var validLines = model.Lines.Where(l => l.ProductId > 0).ToList();

            if (!validLines.Any())
                ModelState.AddModelError(string.Empty, "يجب إضافة صنف واحد على الأقل للسند");

            var duplicateProducts = validLines.GroupBy(l => l.ProductId).Where(g => g.Count() > 1);
            if (duplicateProducts.Any())
                ModelState.AddModelError(string.Empty, "لا يسمح بتكرار نفس الصنف أكثر من مرة في نفس سند الجرد");

            var systemQuantities = new Dictionary<int, decimal>();

            if (validLines.Any())
            {
                var productIds = validLines.Select(l => l.ProductId).Distinct().ToList();
                var activeProducts = await _context.Products
                    .Where(p => productIds.Contains(p.ProductId) && p.IsActive)
                    .ToDictionaryAsync(p => p.ProductId, p => p.CurrentQuantity);

                if (activeProducts.Count != productIds.Count)
                    ModelState.AddModelError(string.Empty, "أحد الأصناف المختارة غير موجود أو موقوف");

                systemQuantities = activeProducts;
            }

            return systemQuantities;
        }

        private static List<StockTakeDetail> BuildDetailsFromModel(StockTakeFormViewModel model, Dictionary<int, decimal> systemQuantities)
        {
            return model.Lines
                .Where(l => l.ProductId > 0)
                .Select(l =>
                {
                    var systemQuantity = systemQuantities.TryGetValue(l.ProductId, out var qty) ? qty : 0;
                    return new StockTakeDetail
                    {
                        ProductId = l.ProductId,
                        SystemQuantity = systemQuantity,
                        ActualQuantity = l.ActualQuantity,
                        Difference = l.ActualQuantity - systemQuantity
                    };
                })
                .ToList();
        }

        private async Task<List<StockTakeProductOption>> GetActiveProductOptionsAsync()
        {
            return await _context.Products.AsNoTracking()
                .Where(p => p.IsActive)
                .OrderBy(p => p.ProductName)
                .Select(p => new StockTakeProductOption
                {
                    ProductId = p.ProductId,
                    ProductName = p.ProductName,
                    CategoryName = p.Category!.CategoryName,
                    Unit = p.Unit,
                    CurrentQuantity = p.CurrentQuantity,
                    Barcode = p.Barcode
                })
                .ToListAsync();
        }
    }
}
