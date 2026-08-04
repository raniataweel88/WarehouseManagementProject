namespace WarehouseUserManagement.Models.ViewModels
{
    // =====================================================================
    // 1) تقرير المخزون الحالي / قيمة المخزون / الأصناف الناقصة
    //    (الفلتر onlyLowStock يحوّل نفس الشاشة إلى "تقرير الأصناف الناقصة")
    // =====================================================================
    public class InventoryReportFilter
    {
        public int? CategoryId { get; set; }
        public bool? IsActive { get; set; }
        public bool OnlyLowStock { get; set; }
        public string? SearchTerm { get; set; }
    }

    public class InventoryReportRow
    {
        public string ProductName { get; set; } = string.Empty;
        public string? Barcode { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public string Unit { get; set; } = string.Empty;
        public decimal CurrentQuantity { get; set; }
        public decimal MinimumQuantity { get; set; }
        public decimal AverageCost { get; set; }
        public decimal TotalValue => CurrentQuantity * AverageCost;
        public bool IsLowStock => CurrentQuantity <= MinimumQuantity;
        public decimal ShortageQuantity => IsLowStock ? MinimumQuantity - CurrentQuantity : 0;
    }

    public class InventoryReportViewModel
    {
        public InventoryReportFilter Filter { get; set; } = new();
        public List<InventoryReportRow> Rows { get; set; } = new();
        public List<Category> Categories { get; set; } = new();
        public int TotalItems => Rows.Count;
        public decimal TotalValue => Rows.Sum(r => r.TotalValue);
        public decimal TotalQuantity => Rows.Sum(r => r.CurrentQuantity);
    }

    // =====================================================================
    // 2) تقرير المشتريات
    // =====================================================================
    public class PurchaseReportFilter
    {
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public string? SupplierName { get; set; }
        public PurchaseInvoiceStatus? Status { get; set; }
    }

    public class PurchaseReportRow
    {
        public string InvoiceNumber { get; set; } = string.Empty;
        public DateTime InvoiceDate { get; set; }
        public string SupplierName { get; set; } = string.Empty;
        public decimal SubTotal { get; set; }
        public decimal SalesTaxAmount { get; set; }
        public decimal GrandTotal { get; set; }
        public PurchaseInvoiceStatus Status { get; set; }
        public string? CreatedBy { get; set; }
    }

    public class PurchaseReportViewModel
    {
        public PurchaseReportFilter Filter { get; set; } = new();
        public List<PurchaseReportRow> Rows { get; set; } = new();
        public int TotalInvoices => Rows.Count;
        public decimal TotalGrandTotal => Rows.Sum(r => r.GrandTotal);
    }

    // =====================================================================
    // 3) تقرير المشتريات حسب المورد
    // =====================================================================
    public class PurchaseBySupplierFilter
    {
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
    }

    public class PurchaseBySupplierRow
    {
        public string SupplierName { get; set; } = string.Empty;
        public int InvoiceCount { get; set; }
        public decimal TotalValue { get; set; }
    }

    public class PurchaseBySupplierViewModel
    {
        public PurchaseBySupplierFilter Filter { get; set; } = new();
        public List<PurchaseBySupplierRow> Rows { get; set; } = new();
        public decimal GrandTotal => Rows.Sum(r => r.TotalValue);
    }

    // =====================================================================
    // 4) تقرير آخر أسعار الشراء
    // =====================================================================
    public class LastPurchasePriceRow
    {
        public string ProductName { get; set; } = string.Empty;
        public string? Barcode { get; set; }
        public string SupplierName { get; set; } = string.Empty;
        public decimal LastUnitPrice { get; set; }
        public DateTime LastPurchaseDate { get; set; }
    }

    public class LastPurchasePriceViewModel
    {
        public string? SearchTerm { get; set; }
        public List<LastPurchasePriceRow> Rows { get; set; } = new();
    }

    // =====================================================================
    // 5) تقرير الصادر حسب القسم
    // =====================================================================
    public class IssueByDepartmentFilter
    {
        public int? DepartmentId { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public int? ProductId { get; set; }
    }

    public class IssueByDepartmentRow
    {
        public string DepartmentName { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public string? Barcode { get; set; }
        public decimal Quantity { get; set; }
        public DateTime IssueDate { get; set; }
        public string ReceiverName { get; set; } = string.Empty;
    }

    public class IssueByDepartmentViewModel
    {
        public IssueByDepartmentFilter Filter { get; set; } = new();
        public List<IssueByDepartmentRow> Rows { get; set; } = new();
        public List<Department> Departments { get; set; } = new();
        public List<Product> Products { get; set; } = new();
        public decimal TotalQuantity => Rows.Sum(r => r.Quantity);
    }

    // =====================================================================
    // 6) تقرير أكثر الأقسام استهلاكاً
    // =====================================================================
    public class DepartmentConsumptionFilter
    {
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
    }

    public class DepartmentConsumptionRow
    {
        public string DepartmentName { get; set; } = string.Empty;
        public decimal TotalQuantity { get; set; }
        public double Percentage { get; set; }
    }

    public class DepartmentConsumptionViewModel
    {
        public DepartmentConsumptionFilter Filter { get; set; } = new();
        public List<DepartmentConsumptionRow> Rows { get; set; } = new();
        public decimal GrandTotal => Rows.Sum(r => r.TotalQuantity);
    }

    // =====================================================================
    // 7) تقرير حركة الصنف (Item Card)
    // =====================================================================
    public class ItemCardFilter
    {
        public int? ProductId { get; set; }
        public int? CategoryId { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
    }

    public class ItemCardRow
    {
        public DateTime TransactionDate { get; set; }
        public string MovementType { get; set; } = string.Empty; // شراء / صرف / تسوية...
        public decimal In { get; set; }
        public decimal Out { get; set; }
        public decimal RunningBalance { get; set; }
        public string ReferenceType { get; set; } = string.Empty;
        public int ReferenceId { get; set; }
    }

    public class ItemCardViewModel
    {
        public ItemCardFilter Filter { get; set; } = new();
        public List<Category> Categories { get; set; } = new();
        public List<Product> Products { get; set; } = new();
        public Product? SelectedProduct { get; set; }
        public List<ItemCardRow> Rows { get; set; } = new();
        public decimal OpeningBalance { get; set; }
        public decimal ClosingBalance => Rows.Any() ? Rows.Last().RunningBalance : OpeningBalance;
    }

    // =====================================================================
    // 8) تقرير الجرد (فروقات)
    // =====================================================================
    public class StockTakeReportFilter
    {
        public int? StockTakeId { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public bool OnlyDifferences { get; set; }
        public string? Barcode { get; set; }
        public int? CategoryId { get; set; }
    }

    public class StockTakeReportRow
    {
        public DateTime StockTakeDate { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string? Barcode { get; set; }
        public string? CategoryName { get; set; }
        public decimal SystemQuantity { get; set; }
        public decimal ActualQuantity { get; set; }
        public decimal Difference { get; set; }
    }

    public class StockTakeReportViewModel
    {
        public StockTakeReportFilter Filter { get; set; } = new();
        public List<StockTake> StockTakes { get; set; } = new();
        public List<Category> Categories { get; set; } = new();
        public List<StockTakeReportRow> Rows { get; set; } = new();
        public decimal TotalShortage => Rows.Where(r => r.Difference < 0).Sum(r => r.Difference);
        public decimal TotalSurplus => Rows.Where(r => r.Difference > 0).Sum(r => r.Difference);
    }

    // =====================================================================
    // 9) تقرير المستخدمين والحركات (User Activity)
    // =====================================================================
    public class UserActivityFilter
    {
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
    }

    public class UserActivityRow
    {
        public string UserName { get; set; } = string.Empty;
        public int ActionsCount { get; set; }
        public DateTime? LastActionDate { get; set; }
    }

    public class UserActivityViewModel
    {
        public UserActivityFilter Filter { get; set; } = new();
        public List<UserActivityRow> Rows { get; set; } = new();
        public int TotalActions => Rows.Sum(r => r.ActionsCount);
    }

    // =====================================================================
    // 10) تقرير ترتيب الأصناف (الأكثر شراءً / الأكثر صرفاً) - عدد الصرف يظهر دائماً لكل صنف
    // =====================================================================
    public class ProductRankingFilter
    {
        public int? CategoryId { get; set; }
        public string? SearchTerm { get; set; }

        // purchased (افتراضي) | issued | name
        public string? SortBy { get; set; }
    }

    public class ProductRankingRow
    {
        public string ProductName { get; set; } = string.Empty;
        public string? Barcode { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public string Unit { get; set; } = string.Empty;
        public decimal CurrentQuantity { get; set; }

        // إجمالي الكمية المشتراة (فواتير معتمدة فقط)
        public decimal TotalPurchasedQuantity { get; set; }

        // إجمالي عدد/كمية الصرف (سندات معتمدة فقط) - يظهر دائماً حتى لو صفر
        public decimal TotalIssuedQuantity { get; set; }
    }

    public class ProductRankingViewModel
    {
        public ProductRankingFilter Filter { get; set; } = new();
        public List<ProductRankingRow> Rows { get; set; } = new();
        public List<Category> Categories { get; set; } = new();
        public decimal GrandTotalPurchased => Rows.Sum(r => r.TotalPurchasedQuantity);
        public decimal GrandTotalIssued => Rows.Sum(r => r.TotalIssuedQuantity);
    }

    // =====================================================================
    // Dashboard
    // =====================================================================
    public class DashboardViewModel
    {
        // Cards
        public int TotalProducts { get; set; }
        public decimal InventoryValue { get; set; }
        public int LowStockCount { get; set; }
        public int PurchaseInvoicesThisMonth { get; set; }
        public int IssuesThisMonth { get; set; }
        public int PendingPurchaseApprovals { get; set; }
        public int PendingIssueVouchers { get; set; }

        // Trend مقارنة بالشهر الماضي (نسبة مئوية، موجبة = ارتفاع)
        public double? PurchaseInvoicesTrend { get; set; }
        public double? IssuesTrend { get; set; }

        // نشاط حديث (آخر 5 عمليات من سجل العمليات)
        public List<RecentActivityItem> RecentActivity { get; set; } = new();

        // Charts
        public List<string> PurchaseMonthLabels { get; set; } = new();
        public List<decimal> PurchaseMonthValues { get; set; } = new();

        public List<string> TopPurchasedProductNames { get; set; } = new();
        public List<decimal> TopPurchasedProductQuantities { get; set; } = new();

        public List<string> TopIssuedProductNames { get; set; } = new();
        public List<decimal> TopIssuedProductQuantities { get; set; } = new();

        // فلتر الفترة الزمنية لشارتي "أكثر الأصناف شراءً / صرفاً" فقط
        public string TopProductsPeriod { get; set; } = "all";
        public DateTime? TopProductsFromDate { get; set; }
        public DateTime? TopProductsToDate { get; set; }

        public List<string> DepartmentConsumptionLabels { get; set; } = new();
        public List<decimal> DepartmentConsumptionValues { get; set; } = new();

        public List<string> StockMovementLabels { get; set; } = new();
        public List<decimal> StockMovementIn { get; set; } = new();
        public List<decimal> StockMovementOut { get; set; } = new();

        // Alerts
        public List<InventoryReportRow> LowStockAlerts { get; set; } = new();
    }

    public class RecentActivityItem
    {
        public string UserName { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }
    }
}