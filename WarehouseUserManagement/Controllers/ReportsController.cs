using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WarehouseUserManagement.Authorization;
using WarehouseUserManagement.Data;
using WarehouseUserManagement.Models;
using WarehouseUserManagement.Models.ViewModels;

namespace WarehouseUserManagement.Controllers
{
    /// <summary>
    /// موديول التقارير - كل تقرير هو Action مستقل بفلاتره الخاصة + تصدير CSV (يفتح مباشرة في Excel).
    /// صلاحيات العرض تُعاد استخدام صلاحيات التقارير الأربع الموجودة أصلاً في DbInitializer
    /// (View Inventory Report / View Purchase Report / View Issue Report / View Audit Log)،
    /// بالإضافة إلى "View StockTake" لتقرير الجرد (لأنه امتداد طبيعي لموديول الجرد الموجود).
    /// </summary>
    [Authorize]
    public class ReportsController : Controller
    {
        private readonly AppDbContext _context;

        public ReportsController(AppDbContext context) => _context = context;

        // GET: /Reports  (قائمة التقارير المتاحة حسب صلاحيات المستخدم - تُفلتر من الـ View نفسه)
        public IActionResult Index() => View();

        // =====================================================================
        // 1) تقرير المخزون الحالي / قيمة المخزون / الأصناف الناقصة
        // =====================================================================
        [HasPermission("View Inventory Report")]
        public async Task<IActionResult> Inventory(InventoryReportFilter filter)
        {
            var rows = await BuildInventoryRowsAsync(filter);

            var vm = new InventoryReportViewModel
            {
                Filter = filter,
                Rows = rows,
                Categories = await _context.Categories.AsNoTracking().Where(c => c.IsActive).OrderBy(c => c.CategoryName).ToListAsync()
            };
            return View(vm);
        }

        [HasPermission("View Inventory Report")]
        public async Task<IActionResult> InventoryCsv(InventoryReportFilter filter)
        {
            var rows = await BuildInventoryRowsAsync(filter);
            var sb = new StringBuilder();
            sb.AppendLine("الصنف,التصنيف,الوحدة,الكمية الحالية,الحد الأدنى,متوسط التكلفة,قيمة المخزون");
            foreach (var r in rows)
                sb.AppendLine($"{Csv(r.ProductName)},{Csv(r.CategoryName)},{Csv(r.Unit)},{r.CurrentQuantity},{r.MinimumQuantity},{r.AverageCost},{r.TotalValue}");

            return CsvFile(sb, filter.OnlyLowStock ? "low-stock-report" : "inventory-report");
        }

        private async Task<List<InventoryReportRow>> BuildInventoryRowsAsync(InventoryReportFilter filter)
        {
            var query = _context.Products.AsNoTracking().Include(p => p.Category).AsQueryable();

            if (filter.CategoryId.HasValue)
                query = query.Where(p => p.CategoryId == filter.CategoryId.Value);

            if (filter.IsActive.HasValue)
                query = query.Where(p => p.IsActive == filter.IsActive.Value);

            if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
            {
                var term = filter.SearchTerm.Trim();
                query = query.Where(p => p.ProductName.Contains(term));
            }

            var rows = await query
                .OrderBy(p => p.Category!.CategoryName).ThenBy(p => p.ProductName)
                .Select(p => new InventoryReportRow
                {
                    ProductName = p.ProductName,
                    CategoryName = p.Category!.CategoryName,
                    Unit = p.Unit,
                    CurrentQuantity = p.CurrentQuantity,
                    MinimumQuantity = p.MinimumQuantity,
                    AverageCost = p.AverageCost
                })
                .ToListAsync();

            if (filter.OnlyLowStock)
                rows = rows.Where(r => r.IsLowStock).ToList();

            return rows;
        }

        // =====================================================================
        // 2) تقرير المشتريات
        // =====================================================================
        [HasPermission("View Purchase Report")]
        public async Task<IActionResult> Purchases(PurchaseReportFilter filter)
        {
            var rows = await BuildPurchaseRowsAsync(filter);
            return View(new PurchaseReportViewModel { Filter = filter, Rows = rows });
        }

        [HasPermission("View Purchase Report")]
        public async Task<IActionResult> PurchasesCsv(PurchaseReportFilter filter)
        {
            var rows = await BuildPurchaseRowsAsync(filter);
            var sb = new StringBuilder();
            sb.AppendLine("رقم الفاتورة,التاريخ,المورد,المجموع الفرعي,الضريبة,الإجمالي,الحالة,المستخدم");
            foreach (var r in rows)
                sb.AppendLine($"{Csv(r.InvoiceNumber)},{r.InvoiceDate:yyyy-MM-dd},{Csv(r.SupplierName)},{r.SubTotal},{r.SalesTaxAmount},{r.GrandTotal},{Csv(StatusLabel(r.Status))},{Csv(r.CreatedBy)}");

            return CsvFile(sb, "purchase-report");
        }

        private async Task<List<PurchaseReportRow>> BuildPurchaseRowsAsync(PurchaseReportFilter filter)
        {
            var query = _context.PurchaseInvoices.AsNoTracking().AsQueryable();

            if (filter.FromDate.HasValue) query = query.Where(i => i.InvoiceDate >= filter.FromDate.Value);
            if (filter.ToDate.HasValue) query = query.Where(i => i.InvoiceDate <= filter.ToDate.Value);
            if (!string.IsNullOrWhiteSpace(filter.SupplierName)) query = query.Where(i => i.SupplierName.Contains(filter.SupplierName));
            if (filter.Status.HasValue) query = query.Where(i => i.Status == filter.Status.Value);

            return await query
                .OrderByDescending(i => i.InvoiceDate)
                .Select(i => new PurchaseReportRow
                {
                    InvoiceNumber = i.InvoiceNumber,
                    InvoiceDate = i.InvoiceDate,
                    SupplierName = i.SupplierName,
                    SubTotal = i.SubTotal,
                    SalesTaxAmount = i.SalesTaxAmount,
                    GrandTotal = i.GrandTotal,
                    Status = i.Status,
                    CreatedBy = i.CreatedBy
                })
                .ToListAsync();
        }

        // =====================================================================
        // 3) تقرير المشتريات حسب المورد
        // =====================================================================
        [HasPermission("View Purchase Report")]
        public async Task<IActionResult> PurchaseBySupplier(PurchaseBySupplierFilter filter)
        {
            var rows = await BuildPurchaseBySupplierRowsAsync(filter);
            return View(new PurchaseBySupplierViewModel { Filter = filter, Rows = rows });
        }

        private async Task<List<PurchaseBySupplierRow>> BuildPurchaseBySupplierRowsAsync(PurchaseBySupplierFilter filter)
        {
            var query = _context.PurchaseInvoices.AsNoTracking()
                .Where(i => i.Status == PurchaseInvoiceStatus.Posted)
                .AsQueryable();

            if (filter.FromDate.HasValue) query = query.Where(i => i.InvoiceDate >= filter.FromDate.Value);
            if (filter.ToDate.HasValue) query = query.Where(i => i.InvoiceDate <= filter.ToDate.Value);

            return await query
                .GroupBy(i => i.SupplierName)
                .Select(g => new PurchaseBySupplierRow
                {
                    SupplierName = g.Key,
                    InvoiceCount = g.Count(),
                    TotalValue = g.Sum(i => i.GrandTotal)
                })
                .OrderByDescending(r => r.TotalValue)
                .ToListAsync();
        }

        // =====================================================================
        // 4) تقرير آخر أسعار الشراء
        // =====================================================================
        [HasPermission("View Purchase Report")]
        public async Task<IActionResult> LastPurchasePrice(string? searchTerm)
        {
            var query = _context.PurchaseInvoiceDetails.AsNoTracking()
                .Include(d => d.Product)
                .Include(d => d.PurchaseInvoice)
                .Where(d => d.PurchaseInvoice!.Status == PurchaseInvoiceStatus.Posted)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchTerm))
                query = query.Where(d => d.Product!.ProductName.Contains(searchTerm));

            // نجلب كل السطور المعتمدة ثم نأخذ آخر سعر لكل صنف بالذاكرة (تفادياً لمحدودية ترجمة
            // GroupBy + OrderBy + First المعقّدة إلى SQL عبر EF Core).
            var allLines = await query
                .Select(d => new
                {
                    d.Product!.ProductName,
                    SupplierName = d.PurchaseInvoice!.SupplierName,
                    d.UnitPrice,
                    d.PurchaseInvoice.InvoiceDate
                })
                .ToListAsync();

            var rows = allLines
                .GroupBy(d => d.ProductName)
                .Select(g =>
                {
                    var last = g.OrderByDescending(d => d.InvoiceDate).First();
                    return new LastPurchasePriceRow
                    {
                        ProductName = g.Key,
                        SupplierName = last.SupplierName,
                        LastUnitPrice = last.UnitPrice,
                        LastPurchaseDate = last.InvoiceDate
                    };
                })
                .OrderBy(r => r.ProductName)
                .ToList();

            return View(new LastPurchasePriceViewModel { SearchTerm = searchTerm, Rows = rows });
        }

        // =====================================================================
        // 5) تقرير الصادر حسب القسم
        // =====================================================================
        [HasPermission("View Issue Report")]
        public async Task<IActionResult> IssueByDepartment(IssueByDepartmentFilter filter)
        {
            var rows = await BuildIssueByDepartmentRowsAsync(filter);

            var vm = new IssueByDepartmentViewModel
            {
                Filter = filter,
                Rows = rows,
                Departments = await _context.Departments.AsNoTracking().Where(d => d.IsActive).OrderBy(d => d.DepartmentName).ToListAsync(),
                Products = await _context.Products.AsNoTracking().Where(p => p.IsActive).OrderBy(p => p.ProductName).ToListAsync()
            };
            return View(vm);
        }

        [HasPermission("View Issue Report")]
        public async Task<IActionResult> IssueByDepartmentCsv(IssueByDepartmentFilter filter)
        {
            var rows = await BuildIssueByDepartmentRowsAsync(filter);
            var sb = new StringBuilder();
            sb.AppendLine("القسم,الصنف,الكمية,التاريخ,المستلم");
            foreach (var r in rows)
                sb.AppendLine($"{Csv(r.DepartmentName)},{Csv(r.ProductName)},{r.Quantity},{r.IssueDate:yyyy-MM-dd},{Csv(r.ReceiverName)}");

            return CsvFile(sb, "issue-by-department");
        }

        private async Task<List<IssueByDepartmentRow>> BuildIssueByDepartmentRowsAsync(IssueByDepartmentFilter filter)
        {
            var query = _context.IssueDetails.AsNoTracking()
                .Include(d => d.Issue).ThenInclude(i => i!.Department)
                .Include(d => d.Product)
                .Where(d => d.Issue!.Status == IssueStatus.Posted)
                .AsQueryable();

            if (filter.DepartmentId.HasValue) query = query.Where(d => d.Issue!.DepartmentId == filter.DepartmentId.Value);
            if (filter.ProductId.HasValue) query = query.Where(d => d.ProductId == filter.ProductId.Value);
            if (filter.FromDate.HasValue) query = query.Where(d => d.Issue!.IssueDate >= filter.FromDate.Value);
            if (filter.ToDate.HasValue) query = query.Where(d => d.Issue!.IssueDate <= filter.ToDate.Value);

            return await query
                .OrderByDescending(d => d.Issue!.IssueDate)
                .Select(d => new IssueByDepartmentRow
                {
                    DepartmentName = d.Issue!.Department!.DepartmentName,
                    ProductName = d.Product!.ProductName,
                    Quantity = d.Quantity,
                    IssueDate = d.Issue.IssueDate,
                    ReceiverName = d.Issue.ReceiverName
                })
                .ToListAsync();
        }

        // =====================================================================
        // 6) تقرير أكثر الأقسام استهلاكاً
        // =====================================================================
        [HasPermission("View Issue Report")]
        public async Task<IActionResult> DepartmentConsumption(DepartmentConsumptionFilter filter)
        {
            var query = _context.IssueDetails.AsNoTracking()
                .Include(d => d.Issue).ThenInclude(i => i!.Department)
                .Where(d => d.Issue!.Status == IssueStatus.Posted)
                .AsQueryable();

            if (filter.FromDate.HasValue) query = query.Where(d => d.Issue!.IssueDate >= filter.FromDate.Value);
            if (filter.ToDate.HasValue) query = query.Where(d => d.Issue!.IssueDate <= filter.ToDate.Value);

            var grouped = await query
                .GroupBy(d => d.Issue!.Department!.DepartmentName)
                .Select(g => new { DepartmentName = g.Key, TotalQuantity = g.Sum(d => d.Quantity) })
                .OrderByDescending(g => g.TotalQuantity)
                .ToListAsync();

            var grandTotal = grouped.Sum(g => g.TotalQuantity);
            var rows = grouped.Select(g => new DepartmentConsumptionRow
            {
                DepartmentName = g.DepartmentName,
                TotalQuantity = g.TotalQuantity,
                Percentage = grandTotal > 0 ? (double)(g.TotalQuantity / grandTotal) * 100 : 0
            }).ToList();

            return View(new DepartmentConsumptionViewModel { Filter = filter, Rows = rows });
        }

        // =====================================================================
        // 7) تقرير حركة الصنف (Item Card / كشف حساب صنف)
        // =====================================================================
        [HasPermission("View Inventory Report")]
        public async Task<IActionResult> ItemCard(ItemCardFilter filter)
        {
            var vm = new ItemCardViewModel
            {
                Filter = filter,
                Products = await _context.Products.AsNoTracking().OrderBy(p => p.ProductName).ToListAsync()
            };

            if (filter.ProductId.HasValue)
            {
                vm.SelectedProduct = await _context.Products.AsNoTracking().FirstOrDefaultAsync(p => p.ProductId == filter.ProductId.Value);

                var query = _context.StockTransactions.AsNoTracking()
                    .Where(t => t.ProductId == filter.ProductId.Value)
                    .AsQueryable();

                // الرصيد الافتتاحي = مجموع كل الحركات قبل تاريخ "من" (إن وُجد)
                if (filter.FromDate.HasValue)
                {
                    vm.OpeningBalance = await _context.StockTransactions.AsNoTracking()
                        .Where(t => t.ProductId == filter.ProductId.Value && t.TransactionDate < filter.FromDate.Value)
                        .SumAsync(t => (decimal?)t.Quantity) ?? 0;
                    query = query.Where(t => t.TransactionDate >= filter.FromDate.Value);
                }

                if (filter.ToDate.HasValue) query = query.Where(t => t.TransactionDate <= filter.ToDate.Value);

                var transactions = await query.OrderBy(t => t.TransactionDate).ThenBy(t => t.StockTransactionId).ToListAsync();

                var balance = vm.OpeningBalance;
                var rows = new List<ItemCardRow>();
                foreach (var t in transactions)
                {
                    balance += t.Quantity;
                    rows.Add(new ItemCardRow
                    {
                        TransactionDate = t.TransactionDate,
                        MovementType = MovementLabel(t.TransactionType),
                        In = t.Quantity > 0 ? t.Quantity : 0,
                        Out = t.Quantity < 0 ? -t.Quantity : 0,
                        RunningBalance = balance,
                        ReferenceType = t.ReferenceType,
                        ReferenceId = t.ReferenceId
                    });
                }
                vm.Rows = rows;
            }

            return View(vm);
        }

        // =====================================================================
        // 8) تقرير الجرد (نتائج وفروقات)
        // =====================================================================
        [HasPermission("View StockTake")]
        public async Task<IActionResult> StockTakeReport(StockTakeReportFilter filter)
        {
            var query = _context.StockTakeDetails.AsNoTracking()
                .Include(d => d.StockTake)
                .Include(d => d.Product)
                .Where(d => d.StockTake!.Status == StockTakeStatus.Posted)
                .AsQueryable();

            if (filter.StockTakeId.HasValue) query = query.Where(d => d.StockTakeId == filter.StockTakeId.Value);
            if (filter.FromDate.HasValue) query = query.Where(d => d.StockTake!.StockTakeDate >= filter.FromDate.Value);
            if (filter.ToDate.HasValue) query = query.Where(d => d.StockTake!.StockTakeDate <= filter.ToDate.Value);
            if (filter.OnlyDifferences) query = query.Where(d => d.Difference != 0);

            var rows = await query
                .OrderByDescending(d => d.StockTake!.StockTakeDate)
                .Select(d => new StockTakeReportRow
                {
                    StockTakeDate = d.StockTake!.StockTakeDate,
                    ProductName = d.Product!.ProductName,
                    SystemQuantity = d.SystemQuantity,
                    ActualQuantity = d.ActualQuantity,
                    Difference = d.Difference
                })
                .ToListAsync();

            var vm = new StockTakeReportViewModel
            {
                Filter = filter,
                Rows = rows,
                StockTakes = await _context.StockTakes.AsNoTracking()
                    .Where(s => s.Status == StockTakeStatus.Posted)
                    .OrderByDescending(s => s.StockTakeDate)
                    .ToListAsync()
            };
            return View(vm);
        }

        // =====================================================================
        // 9) تقرير المستخدمين والحركات (User Activity) - ملخص فوق سجل العمليات الموجود
        // =====================================================================
        [HasPermission("View Audit Log")]
        public async Task<IActionResult> UserActivity(UserActivityFilter filter)
        {
            var query = _context.AuditLogs.AsNoTracking().Include(a => a.User).AsQueryable();

            if (filter.FromDate.HasValue) query = query.Where(a => a.CreatedDate >= filter.FromDate.Value);
            if (filter.ToDate.HasValue) query = query.Where(a => a.CreatedDate <= filter.ToDate.Value);

            var rows = await query
                .GroupBy(a => a.User != null ? a.User.FullName : "غير معروف")
                .Select(g => new UserActivityRow
                {
                    UserName = g.Key,
                    ActionsCount = g.Count(),
                    LastActionDate = g.Max(a => a.CreatedDate)
                })
                .OrderByDescending(r => r.ActionsCount)
                .ToListAsync();

            return View(new UserActivityViewModel { Filter = filter, Rows = rows });
        }

        // ===== Helpers =====

        private static string MovementLabel(StockTransactionType type) => type switch
        {
            StockTransactionType.Purchase => "شراء",
            StockTransactionType.PurchaseReverse => "إلغاء شراء",
            StockTransactionType.Issue => "صرف",
            StockTransactionType.IssueReverse => "إلغاء صرف",
            StockTransactionType.Adjustment => "تسوية جرد",
            _ => type.ToString()
        };

        private static string StatusLabel(PurchaseInvoiceStatus s) => s switch
        {
            PurchaseInvoiceStatus.Draft => "مسودة",
            PurchaseInvoiceStatus.Posted => "معتمدة",
            PurchaseInvoiceStatus.Cancelled => "ملغاة",
            _ => s.ToString()
        };

        /// <summary>يهرب أي فاصلة أو علامة اقتباس داخل قيمة قبل كتابتها في CSV.</summary>
        private static string Csv(string? value)
        {
            value ??= string.Empty;
            if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
                return "\"" + value.Replace("\"", "\"\"") + "\"";
            return value;
        }

        /// <summary>يبني ملف CSV بترميز UTF-8 مع BOM حتى يفتح بالعربي بشكل صحيح داخل Excel مباشرة.</summary>
        private FileContentResult CsvFile(StringBuilder sb, string fileNamePrefix)
        {
            var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
            var fileName = $"{fileNamePrefix}-{DateTime.Now:yyyyMMdd-HHmm}.csv";
            return File(bytes, "text/csv; charset=utf-8", fileName);
        }
    }
}
