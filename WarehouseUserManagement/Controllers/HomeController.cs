 
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WarehouseUserManagement.Data;
using WarehouseUserManagement.Models;
using WarehouseUserManagement.Models.ViewModels;

namespace WarehouseUserManagement.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly AppDbContext _context;

        public HomeController(AppDbContext context) => _context = context;

        public async Task<IActionResult> Index()
        {
            // إحصائيات سريعة للوحة التحكم - استعلامات Count بسيطة تستفيد من الفهارس
            ViewBag.TotalUsers = await _context.Users.CountAsync();
            ViewBag.ActiveUsers = await _context.Users.CountAsync(u => u.IsActive);
            ViewBag.LockedUsers = await _context.Users.CountAsync(u => u.IsLocked);
            ViewBag.TotalRoles = await _context.Roles.CountAsync();
            return View();
        }

        // لوحة تحكم Module 10: بطاقات + Charts + تنبيهات، حسب تحليل موديول الـ Reports/Dashboard.
        // topPeriod: all | month | 3m | 6m | 12m | custom - يتحكم بفترة شارتي "أكثر الأصناف شراءً / صرفاً" فقط
        public async Task<IActionResult> Dashboard(string? topPeriod, DateTime? topFrom, DateTime? topTo)
        {
            var now = DateTime.UtcNow;
            var monthStart = new DateTime(now.Year, now.Month, 1);

            var vm = new DashboardViewModel
            {
                TotalProducts = await _context.Products.CountAsync(p => p.IsActive),
                PurchaseInvoicesThisMonth = await _context.PurchaseInvoices.CountAsync(i => i.InvoiceDate >= monthStart),
                IssuesThisMonth = await _context.Issues.CountAsync(i => i.IssueDate >= monthStart),
                PendingPurchaseApprovals = await _context.PurchaseInvoices.CountAsync(i => i.Status == PurchaseInvoiceStatus.Draft),
                PendingIssueVouchers = await _context.Issues.CountAsync(i => i.Status == IssueStatus.Draft)
            };

            // ===== مقارنة بالشهر الماضي (Trend) =====
            var lastMonthStart = monthStart.AddMonths(-1);
            var lastMonthPurchases = await _context.PurchaseInvoices.CountAsync(i => i.InvoiceDate >= lastMonthStart && i.InvoiceDate < monthStart);
            var lastMonthIssues = await _context.Issues.CountAsync(i => i.IssueDate >= lastMonthStart && i.IssueDate < monthStart);

            vm.PurchaseInvoicesTrend = lastMonthPurchases > 0
                ? Math.Round((vm.PurchaseInvoicesThisMonth - lastMonthPurchases) / (double)lastMonthPurchases * 100, 1)
                : null;
            vm.IssuesTrend = lastMonthIssues > 0
                ? Math.Round((vm.IssuesThisMonth - lastMonthIssues) / (double)lastMonthIssues * 100, 1)
                : null;

            // ===== آخر نشاط (سجل العمليات) =====
            vm.RecentActivity = await _context.AuditLogs.AsNoTracking()
                .Include(a => a.User)
                .OrderByDescending(a => a.CreatedDate)
                .Take(5)
                .Select(a => new RecentActivityItem
                {
                    UserName = a.User != null ? a.User.FullName : "النظام",
                    Action = a.Action,
                    CreatedDate = a.CreatedDate
                })
                .ToListAsync();

            // ===== بطاقات المخزون =====
            var products = await _context.Products.AsNoTracking()
                .Where(p => p.IsActive)
                .Select(p => new { p.ProductName, p.CurrentQuantity, p.MinimumQuantity, p.AverageCost })
                .ToListAsync();

            vm.InventoryValue = products.Sum(p => p.CurrentQuantity * p.AverageCost);
            var lowStock = products.Where(p => p.CurrentQuantity <= p.MinimumQuantity).ToList();
            vm.LowStockCount = lowStock.Count;
            vm.LowStockAlerts = lowStock
                .OrderBy(p => p.CurrentQuantity - p.MinimumQuantity)
                .Take(10)
                .Select(p => new InventoryReportRow
                {
                    ProductName = p.ProductName,
                    CurrentQuantity = p.CurrentQuantity,
                    MinimumQuantity = p.MinimumQuantity,
                    AverageCost = p.AverageCost
                })
                .ToList();

            // ===== المشتريات حسب الشهر (آخر 6 أشهر) =====
            var sixMonthsAgo = new DateTime(now.Year, now.Month, 1).AddMonths(-5);
            var purchasesByMonth = await _context.PurchaseInvoices.AsNoTracking()
                .Where(i => i.Status == PurchaseInvoiceStatus.Posted && i.InvoiceDate >= sixMonthsAgo)
                .GroupBy(i => new { i.InvoiceDate.Year, i.InvoiceDate.Month })
                .Select(g => new { g.Key.Year, g.Key.Month, Total = g.Sum(i => i.GrandTotal) })
                .ToListAsync();

            for (var m = 0; m < 6; m++)
            {
                var month = sixMonthsAgo.AddMonths(m);
                var match = purchasesByMonth.FirstOrDefault(x => x.Year == month.Year && x.Month == month.Month);
                vm.PurchaseMonthLabels.Add(month.ToString("MMM yyyy"));
                vm.PurchaseMonthValues.Add(match?.Total ?? 0);
            }

            // ===== فترة شارتي "أكثر الأصناف شراءً / صرفاً" =====
            var period = string.IsNullOrWhiteSpace(topPeriod) ? "all" : topPeriod.Trim().ToLowerInvariant();
            DateTime? topRangeFrom = period switch
            {
                "month" => monthStart,
                "3m" => monthStart.AddMonths(-2),
                "6m" => monthStart.AddMonths(-5),
                "12m" => monthStart.AddMonths(-11),
                "custom" => topFrom,
                _ => null
            };
            DateTime? topRangeTo = period == "custom" ? topTo : null;

            vm.TopProductsPeriod = period;
            vm.TopProductsFromDate = topFrom;
            vm.TopProductsToDate = topTo;

            // ===== أكثر الأصناف شراءً (أعلى 5 بالكمية) =====
            var topPurchasedQuery = _context.PurchaseInvoiceDetails.AsNoTracking()
                .Include(d => d.PurchaseInvoice)
                .Include(d => d.Product)
                .Where(d => d.PurchaseInvoice!.Status == PurchaseInvoiceStatus.Posted);
            if (topRangeFrom.HasValue) topPurchasedQuery = topPurchasedQuery.Where(d => d.PurchaseInvoice!.InvoiceDate >= topRangeFrom.Value);
            if (topRangeTo.HasValue) topPurchasedQuery = topPurchasedQuery.Where(d => d.PurchaseInvoice!.InvoiceDate <= topRangeTo.Value);

            var topPurchased = await topPurchasedQuery
                .GroupBy(d => d.Product!.ProductName)
                .Select(g => new { ProductName = g.Key, TotalQuantity = g.Sum(d => d.Quantity) })
                .OrderByDescending(g => g.TotalQuantity)
                .Take(5)
                .ToListAsync();

            vm.TopPurchasedProductNames = topPurchased.Select(p => p.ProductName).ToList();
            vm.TopPurchasedProductQuantities = topPurchased.Select(p => p.TotalQuantity).ToList();

            // ===== أكثر الأصناف صرفاً (أعلى 5 بالكمية) =====
            var topIssuedQuery = _context.IssueDetails.AsNoTracking()
                .Include(d => d.Issue)
                .Include(d => d.Product)
                .Where(d => d.Issue!.Status == IssueStatus.Posted);
            if (topRangeFrom.HasValue) topIssuedQuery = topIssuedQuery.Where(d => d.Issue!.IssueDate >= topRangeFrom.Value);
            if (topRangeTo.HasValue) topIssuedQuery = topIssuedQuery.Where(d => d.Issue!.IssueDate <= topRangeTo.Value);

            var topIssued = await topIssuedQuery
                .GroupBy(d => d.Product!.ProductName)
                .Select(g => new { ProductName = g.Key, TotalQuantity = g.Sum(d => d.Quantity) })
                .OrderByDescending(g => g.TotalQuantity)
                .Take(5)
                .ToListAsync();

            vm.TopIssuedProductNames = topIssued.Select(p => p.ProductName).ToList();
            vm.TopIssuedProductQuantities = topIssued.Select(p => p.TotalQuantity).ToList();

            // ===== أكثر الأقسام استهلاكاً =====
            var deptConsumptionQuery = _context.IssueDetails.AsNoTracking()
                .Include(d => d.Issue).ThenInclude(i => i!.Department)
                .Where(d => d.Issue!.Status == IssueStatus.Posted);
            if (topRangeFrom.HasValue) deptConsumptionQuery = deptConsumptionQuery.Where(d => d.Issue!.IssueDate >= topRangeFrom.Value);
            if (topRangeTo.HasValue) deptConsumptionQuery = deptConsumptionQuery.Where(d => d.Issue!.IssueDate <= topRangeTo.Value);

            var deptConsumption = await deptConsumptionQuery
                .GroupBy(d => d.Issue!.Department!.DepartmentName)
                .Select(g => new { DepartmentName = g.Key, TotalQuantity = g.Sum(d => d.Quantity) })
                .OrderByDescending(g => g.TotalQuantity)
                .Take(6)
                .ToListAsync();

            vm.DepartmentConsumptionLabels = deptConsumption.Select(d => d.DepartmentName).ToList();
            vm.DepartmentConsumptionValues = deptConsumption.Select(d => d.TotalQuantity).ToList();

            // ===== حركة المخزون (وارد/صادر) آخر 6 أشهر =====
            var movementRaw = await _context.StockTransactions.AsNoTracking()
                .Where(t => t.TransactionDate >= sixMonthsAgo)
                .Select(t => new { t.TransactionDate.Year, t.TransactionDate.Month, t.Quantity })
                .ToListAsync();

            for (var m = 0; m < 6; m++)
            {
                var month = sixMonthsAgo.AddMonths(m);
                var monthItems = movementRaw.Where(x => x.Year == month.Year && x.Month == month.Month).ToList();
                vm.StockMovementLabels.Add(month.ToString("MMM yyyy"));
                vm.StockMovementIn.Add(monthItems.Where(x => x.Quantity > 0).Sum(x => x.Quantity));
                vm.StockMovementOut.Add(-monthItems.Where(x => x.Quantity < 0).Sum(x => x.Quantity));
            }

            return View(vm);
        }
    }
}