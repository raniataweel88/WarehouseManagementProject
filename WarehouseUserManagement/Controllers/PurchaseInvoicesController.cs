using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WarehouseUserManagement.Authorization;
using WarehouseUserManagement.Data;
using WarehouseUserManagement.Models;
using WarehouseUserManagement.Models.ViewModels;
using WarehouseUserManagement.Services;

namespace WarehouseUserManagement.Controllers
{
    public class PurchaseInvoicesController : Controller
    {
        private readonly AppDbContext _context;
        private readonly ICurrentUserService _currentUser;

        public PurchaseInvoicesController(AppDbContext context, ICurrentUserService currentUser)
        {
            _context = context;
            _currentUser = currentUser;
        }

        // GET: /PurchaseInvoices?searchTerm=&statusFilter=&pageNumber=1
        [HasPermission("View Purchase Invoices")]
        public async Task<IActionResult> Index(PurchaseInvoiceSearchRequest request)
        {
            var query = _context.PurchaseInvoices.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(request.SearchTerm))
            {
                var term = request.SearchTerm.Trim();
                query = query.Where(i => i.InvoiceNumber.StartsWith(term) || i.SupplierName.StartsWith(term));
            }

            if (request.StatusFilter.HasValue)
                query = query.Where(i => i.Status == request.StatusFilter.Value);

            var totalCount = await query.CountAsync();

            var pageSize = request.PageSize is > 0 and <= 100 ? request.PageSize : 10;
            var pageNumber = request.PageNumber < 1 ? 1 : request.PageNumber;

            var invoices = await query
                .OrderByDescending(i => i.InvoiceDate).ThenByDescending(i => i.PurchaseInvoiceId)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(i => new PurchaseInvoiceRow
                {
                    PurchaseInvoiceId = i.PurchaseInvoiceId,
                    InvoiceNumber = i.InvoiceNumber,
                    InvoiceDate = i.InvoiceDate,
                    SupplierName = i.SupplierName,
                    GrandTotal = i.GrandTotal,
                    Status = i.Status
                })
                .ToListAsync();

            var vm = new PurchaseInvoiceListViewModel
            {
                Invoices = invoices,
                SearchTerm = request.SearchTerm,
                StatusFilter = request.StatusFilter,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalCount = totalCount
            };

            return View(vm);
        }

        // GET: /PurchaseInvoices/Create
        [HasPermission("Create Purchase Invoice")]
        public async Task<IActionResult> Create()
        {
            var vm = new PurchaseInvoiceFormViewModel
            {
                InvoiceNumber = await GenerateNextInvoiceNumberAsync(),
                ProductOptions = await GetActiveProductOptionsAsync(),
                CategoryOptions = await GetActiveCategoryOptionsAsync(),
                Lines = new List<PurchaseInvoiceLineViewModel> { new() }
            };
            return View(vm);
        }

        // POST: /PurchaseInvoices/Create  (يُنشئ الفاتورة بحالة Draft)
        [HttpPost]
        [ValidateAntiForgeryToken]
        [HasPermission("Create Purchase Invoice")]
        public async Task<IActionResult> Create(PurchaseInvoiceFormViewModel model)
        {
            await ValidateInvoiceAsync(model, invoiceIdToExclude: null);

            if (!ModelState.IsValid)
            {
                model.ProductOptions = await GetActiveProductOptionsAsync();
                model.CategoryOptions = await GetActiveCategoryOptionsAsync();
                return View(model);
            }

            var invoice = BuildInvoiceFromModel(model);
            invoice.Status = PurchaseInvoiceStatus.Draft;
            invoice.CreatedBy = _currentUser.Username;
            invoice.CreatedDate = DateTime.UtcNow;

            _context.PurchaseInvoices.Add(invoice);
            await _context.SaveChangesAsync();

            TempData["Success"] = "تم حفظ فاتورة الشراء كمسودة";
            return RedirectToAction(nameof(Index));
        }

        // GET: /PurchaseInvoices/Edit/5  (يعمل فقط للفواتير بحالة Draft)
        [HasPermission("Edit Purchase Invoice")]
        public async Task<IActionResult> Edit(int id)
        {
            var invoice = await _context.PurchaseInvoices.AsNoTracking()
                .Include(i => i.Details)
                .FirstOrDefaultAsync(i => i.PurchaseInvoiceId == id);
            if (invoice is null) return NotFound();

            if (invoice.Status != PurchaseInvoiceStatus.Draft)
            {
                TempData["Error"] = "لا يمكن تعديل فاتورة معتمدة أو ملغاة";
                return RedirectToAction(nameof(Index));
            }

            var vm = new PurchaseInvoiceFormViewModel
            {
                PurchaseInvoiceId = invoice.PurchaseInvoiceId,
                InvoiceNumber = invoice.InvoiceNumber,
                InvoiceDate = invoice.InvoiceDate,
                SupplierName = invoice.SupplierName,
                SupplierPhone = invoice.SupplierPhone,
                SupplierAddress = invoice.SupplierAddress,
                Notes = invoice.Notes,
                SalesTaxPercent = invoice.SalesTaxPercent,
                RowVersion = invoice.RowVersion,
                ProductOptions = await GetActiveProductOptionsAsync(),
                CategoryOptions = await GetActiveCategoryOptionsAsync(),
                Lines = invoice.Details.Select(d => new PurchaseInvoiceLineViewModel
                {
                    ProductId = d.ProductId,
                    Quantity = d.Quantity,
                    UnitPrice = d.UnitPrice
                }).ToList()
            };

            return View(vm);
        }

        // POST: /PurchaseInvoices/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [HasPermission("Edit Purchase Invoice")]
        public async Task<IActionResult> Edit(int id, PurchaseInvoiceFormViewModel model)
        {
            if (id != model.PurchaseInvoiceId) return NotFound();

            var invoice = await _context.PurchaseInvoices
                .Include(i => i.Details)
                .FirstOrDefaultAsync(i => i.PurchaseInvoiceId == id);
            if (invoice is null) return NotFound();

            if (invoice.Status != PurchaseInvoiceStatus.Draft)
            {
                TempData["Error"] = "لا يمكن تعديل فاتورة معتمدة أو ملغاة";
                return RedirectToAction(nameof(Index));
            }

            await ValidateInvoiceAsync(model, invoiceIdToExclude: id);

            if (!ModelState.IsValid)
            {
                model.ProductOptions = await GetActiveProductOptionsAsync();
                model.CategoryOptions = await GetActiveCategoryOptionsAsync();
                return View(model);
            }

            if (model.RowVersion is not null)
                _context.Entry(invoice).Property(i => i.RowVersion).OriginalValue = model.RowVersion;

            invoice.InvoiceNumber = model.InvoiceNumber;
            invoice.InvoiceDate = model.InvoiceDate;
            invoice.SupplierName = model.SupplierName;
            invoice.SupplierPhone = model.SupplierPhone;
            invoice.SupplierAddress = model.SupplierAddress;
            invoice.Notes = model.Notes;
            invoice.SalesTaxPercent = model.SalesTaxPercent;

            // استبدال كل سطور الفاتورة بالسطور الجديدة (الفاتورة ما زالت Draft، لا تأثير على المخزون بعد)
            _context.PurchaseInvoiceDetails.RemoveRange(invoice.Details);
            invoice.Details = BuildDetailsFromModel(model);
            RecalculateTotals(invoice);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                ModelState.AddModelError(string.Empty, "تم تعديل هذه الفاتورة من جلسة أخرى، الرجاء إعادة تحميل الصفحة");
                model.ProductOptions = await GetActiveProductOptionsAsync();
                model.CategoryOptions = await GetActiveCategoryOptionsAsync();
                return View(model);
            }

            TempData["Success"] = "تم تعديل فاتورة الشراء بنجاح";
            return RedirectToAction(nameof(Index));
        }

        // GET: /PurchaseInvoices/Details/5
        [HasPermission("View Purchase Invoices")]
        public async Task<IActionResult> Details(int id)
        {
            var invoice = await _context.PurchaseInvoices.AsNoTracking()
                .Include(i => i.Details).ThenInclude(d => d.Product)
                .FirstOrDefaultAsync(i => i.PurchaseInvoiceId == id);
            if (invoice is null) return NotFound();

            var vm = new PurchaseInvoiceDetailsViewModel
            {
                PurchaseInvoiceId = invoice.PurchaseInvoiceId,
                InvoiceNumber = invoice.InvoiceNumber,
                InvoiceDate = invoice.InvoiceDate,
                SupplierName = invoice.SupplierName,
                SupplierPhone = invoice.SupplierPhone,
                SupplierAddress = invoice.SupplierAddress,
                Notes = invoice.Notes,
                SubTotal = invoice.SubTotal,
                SalesTaxPercent = invoice.SalesTaxPercent,
                SalesTaxAmount = invoice.SalesTaxAmount,
                GrandTotal = invoice.GrandTotal,
                Status = invoice.Status,
                CreatedBy = invoice.CreatedBy,
                CreatedDate = invoice.CreatedDate,
                PostedBy = invoice.PostedBy,
                PostedDate = invoice.PostedDate,
                CancelledBy = invoice.CancelledBy,
                CancelledDate = invoice.CancelledDate,
                Lines = invoice.Details.Select(d => new PurchaseInvoiceDetailRow
                {
                    ProductName = d.Product!.ProductName,
                    Unit = d.Product.Unit,
                    Quantity = d.Quantity,
                    UnitPrice = d.UnitPrice,
                    LineTotal = d.LineTotal
                }).ToList()
            };

            return View(vm);
        }

        // POST: /PurchaseInvoices/Approve/5  (Draft -> Posted: يحدّث المخزون ومتوسط التكلفة وينشئ حركة مخزون)
        [HttpPost]
        [ValidateAntiForgeryToken]
        [HasPermission("Post Purchase Invoice")]
        public async Task<IActionResult> Approve(int id)
        {
            var invoice = await _context.PurchaseInvoices
                .Include(i => i.Details).ThenInclude(d => d.Product)
                .FirstOrDefaultAsync(i => i.PurchaseInvoiceId == id);
            if (invoice is null) return NotFound();

            if (invoice.Status != PurchaseInvoiceStatus.Draft)
            {
                TempData["Error"] = "لا يمكن اعتماد فاتورة إلا وهي بحالة مسودة";
                return RedirectToAction(nameof(Index));
            }

            if (!invoice.Details.Any())
            {
                TempData["Error"] = "لا يمكن اعتماد فاتورة لا تحتوي على أصناف";
                return RedirectToAction(nameof(Index));
            }

            foreach (var line in invoice.Details)
            {
                var product = line.Product!;

                // متوسط التكلفة المرجّح (Weighted Average Cost)
                var existingValue = product.CurrentQuantity * product.AverageCost;
                var incomingValue = line.Quantity * line.UnitPrice;
                var newQuantity = product.CurrentQuantity + line.Quantity;

                product.AverageCost = newQuantity > 0
                    ? Math.Round((existingValue + incomingValue) / newQuantity, 4)
                    : 0;
                product.CurrentQuantity = newQuantity;
                product.UpdatedBy = _currentUser.Username;
                product.UpdatedDate = DateTime.UtcNow;

                _context.StockTransactions.Add(new StockTransaction
                {
                    ProductId = product.ProductId,
                    TransactionType = StockTransactionType.Purchase,
                    Quantity = line.Quantity,
                    UnitCost = line.UnitPrice,
                    ReferenceType = "PurchaseInvoice",
                    ReferenceId = invoice.PurchaseInvoiceId,
                    TransactionDate = DateTime.UtcNow,
                    CreatedBy = _currentUser.Username
                });
            }

            invoice.Status = PurchaseInvoiceStatus.Posted;
            invoice.PostedBy = _currentUser.Username;
            invoice.PostedDate = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            TempData["Success"] = "تم اعتماد فاتورة الشراء وتحديث المخزون";
            return RedirectToAction(nameof(Index));
        }

        // POST: /PurchaseInvoices/Cancel/5
        // Draft -> Cancelled: إلغاء بسيط بدون أي أثر على المخزون (لم يُصرف بعد)
        // Posted -> Cancelled: عكس الكميات المضافة سابقاً وإنشاء حركة مخزون عكسية
        [HttpPost]
        [ValidateAntiForgeryToken]
        [HasPermission("Cancel Purchase Invoice")]
        public async Task<IActionResult> Cancel(int id)
        {
            var invoice = await _context.PurchaseInvoices
                .Include(i => i.Details).ThenInclude(d => d.Product)
                .FirstOrDefaultAsync(i => i.PurchaseInvoiceId == id);
            if (invoice is null) return NotFound();

            if (invoice.Status == PurchaseInvoiceStatus.Cancelled)
            {
                TempData["Error"] = "الفاتورة ملغاة مسبقاً";
                return RedirectToAction(nameof(Index));
            }

            if (invoice.Status == PurchaseInvoiceStatus.Posted)
            {
                foreach (var line in invoice.Details)
                {
                    var product = line.Product!;

                    // عكس الكمية فقط (لا يمكن عكس متوسط التكلفة المرجّح بدقة كاملة بعد اختلاطه بحركات أخرى،
                    // لذلك تبقى القيمة الحالية لمتوسط التكلفة كما هي كتبسيط مقصود لهذا الإصدار)
                    product.CurrentQuantity = Math.Max(0, product.CurrentQuantity - line.Quantity);
                    product.UpdatedBy = _currentUser.Username;
                    product.UpdatedDate = DateTime.UtcNow;

                    _context.StockTransactions.Add(new StockTransaction
                    {
                        ProductId = product.ProductId,
                        TransactionType = StockTransactionType.PurchaseReverse,
                        Quantity = -line.Quantity,
                        UnitCost = line.UnitPrice,
                        ReferenceType = "PurchaseInvoice",
                        ReferenceId = invoice.PurchaseInvoiceId,
                        TransactionDate = DateTime.UtcNow,
                        CreatedBy = _currentUser.Username
                    });
                }
            }

            invoice.Status = PurchaseInvoiceStatus.Cancelled;
            invoice.CancelledBy = _currentUser.Username;
            invoice.CancelledDate = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            TempData["Success"] = "تم إلغاء فاتورة الشراء";
            return RedirectToAction(nameof(Index));
        }

        // ===== Helpers =====

        private async Task ValidateInvoiceAsync(PurchaseInvoiceFormViewModel model, int? invoiceIdToExclude)
        {
            var numberExists = await _context.PurchaseInvoices.AnyAsync(i =>
                i.InvoiceNumber == model.InvoiceNumber &&
                (invoiceIdToExclude == null || i.PurchaseInvoiceId != invoiceIdToExclude));
            if (numberExists)
                ModelState.AddModelError(nameof(model.InvoiceNumber), "رقم الفاتورة مستخدم مسبقاً");

            var validLines = model.Lines.Where(l => l.ProductId > 0).ToList();

            if (!validLines.Any())
                ModelState.AddModelError(string.Empty, "يجب إضافة صنف واحد على الأقل للفاتورة");

            var duplicateProducts = validLines.GroupBy(l => l.ProductId).Where(g => g.Count() > 1);
            if (duplicateProducts.Any())
                ModelState.AddModelError(string.Empty, "لا يسمح بتكرار نفس الصنف أكثر من مرة في نفس الفاتورة");

            if (validLines.Any())
            {
                var productIds = validLines.Select(l => l.ProductId).Distinct().ToList();
                var activeCount = await _context.Products.CountAsync(p => productIds.Contains(p.ProductId) && p.IsActive);
                if (activeCount != productIds.Count)
                    ModelState.AddModelError(string.Empty, "أحد الأصناف المختارة غير موجود أو موقوف");
            }
        }

        private PurchaseInvoice BuildInvoiceFromModel(PurchaseInvoiceFormViewModel model)
        {
            var invoice = new PurchaseInvoice
            {
                InvoiceNumber = model.InvoiceNumber,
                InvoiceDate = model.InvoiceDate,
                SupplierName = model.SupplierName,
                SupplierPhone = model.SupplierPhone,
                SupplierAddress = model.SupplierAddress,
                Notes = model.Notes,
                SalesTaxPercent = model.SalesTaxPercent,
                Details = BuildDetailsFromModel(model)
            };
            RecalculateTotals(invoice);
            return invoice;
        }

        private static List<PurchaseInvoiceDetail> BuildDetailsFromModel(PurchaseInvoiceFormViewModel model)
        {
            return model.Lines
                .Where(l => l.ProductId > 0)
                .Select(l => new PurchaseInvoiceDetail
                {
                    ProductId = l.ProductId,
                    Quantity = l.Quantity,
                    UnitPrice = l.UnitPrice,
                    LineTotal = Math.Round(l.Quantity * l.UnitPrice, 2)
                })
                .ToList();
        }

        // Sub Total = Sum(Line Totals) | Sales Tax Amount = Sub Total × Tax% | Grand Total = Sub Total + Tax Amount
        private static void RecalculateTotals(PurchaseInvoice invoice)
        {
            invoice.SubTotal = Math.Round(invoice.Details.Sum(d => d.LineTotal), 2);
            invoice.SalesTaxAmount = Math.Round(invoice.SubTotal * (invoice.SalesTaxPercent / 100m), 2);
            invoice.GrandTotal = invoice.SubTotal + invoice.SalesTaxAmount;
        }

        private async Task<List<ProductOption>> GetActiveProductOptionsAsync()
        {
            return await _context.Products.AsNoTracking()
                .Where(p => p.IsActive)
                .OrderBy(p => p.ProductName)
                .Select(p => new ProductOption
                {
                    ProductId = p.ProductId,
                    ProductName = p.ProductName,
                    CategoryName = p.Category!.CategoryName,
                    Unit = p.Unit,
                    AverageCost = p.AverageCost
                })
                .ToListAsync();
        }

        private async Task<List<CategoryOption>> GetActiveCategoryOptionsAsync()
        {
            // تُستخدم لتعبئة قائمة التصنيفات في نافذة "إضافة صنف جديد" السريعة من داخل الفاتورة
            return await _context.Categories.AsNoTracking()
                .Where(c => c.IsActive)
                .OrderBy(c => c.CategoryName)
                .Select(c => new CategoryOption { CategoryId = c.CategoryId, CategoryName = c.CategoryName })
                .ToListAsync();
        }

        // يولّد رقم فاتورة افتراضي متسلسل (PINV-000001, PINV-000002, ...) بحيث يزيد تلقائياً
        // في كل مرة يُفتح فيها نموذج فاتورة جديدة، مع بقاء الحقل قابلاً للتعديل يدوياً إذا رغب المستخدم.
        private async Task<string> GenerateNextInvoiceNumberAsync()
        {


            var lastNumbers = await _context.PurchaseInvoices
                .OrderByDescending(i => i.PurchaseInvoiceId)
                .Select(i => i.InvoiceNumber)
                .Take(20) // نأخذ آخر عدة أرقام احتياطاً لأي رقم أُدخل يدوياً بصيغة غير رقمية بحتة
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