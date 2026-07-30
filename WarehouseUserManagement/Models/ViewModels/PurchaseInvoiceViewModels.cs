using System.ComponentModel.DataAnnotations;

namespace WarehouseUserManagement.Models.ViewModels
{
    public class PurchaseInvoiceFormViewModel
    {
        public int PurchaseInvoiceId { get; set; }

        [Required(ErrorMessage = "رقم الفاتورة مطلوب")]
        [MaxLength(50)]
        [Display(Name = "رقم الفاتورة")]
        public string InvoiceNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "تاريخ الفاتورة مطلوب")]
        [Display(Name = "تاريخ الفاتورة")]
        [DataType(DataType.Date)]
        public DateTime InvoiceDate { get; set; } = DateTime.UtcNow.Date;

        [Required(ErrorMessage = "اسم المورد مطلوب")]
        [MaxLength(150)]
        [Display(Name = "اسم المورد")]
        public string SupplierName { get; set; } = string.Empty;

        [MaxLength(30)]
        [Display(Name = "هاتف المورد")]
        public string? SupplierPhone { get; set; }

        [MaxLength(250)]
        [Display(Name = "عنوان المورد")]
        public string? SupplierAddress { get; set; }

        [MaxLength(500)]
        [Display(Name = "ملاحظات")]
        public string? Notes { get; set; }

        [Range(0, 100, ErrorMessage = "نسبة الضريبة يجب أن تكون بين 0 و 100")]
        [Display(Name = "نسبة الضريبة %")]
        public decimal SalesTaxPercent { get; set; }

        public byte[]? RowVersion { get; set; }

        public List<PurchaseInvoiceLineViewModel> Lines { get; set; } = new();

        public List<ProductOption> ProductOptions { get; set; } = new();
        public List<CategoryOption> CategoryOptions { get; set; } = new();
    }

    public class PurchaseInvoiceLineViewModel
    {
        [Required(ErrorMessage = "الصنف مطلوب")]
        [Display(Name = "الصنف")]
        public int ProductId { get; set; }

        [Range(0.001, double.MaxValue, ErrorMessage = "الكمية يجب أن تكون أكبر من صفر")]
        [Display(Name = "الكمية")]
        public decimal Quantity { get; set; }

        [Range(0.01, double.MaxValue, ErrorMessage = "سعر الوحدة يجب أن يكون أكبر من صفر")]
        [Display(Name = "سعر الوحدة")]
        public decimal UnitPrice { get; set; }
    }

    public class PurchaseInvoiceListViewModel
    {
        public List<PurchaseInvoiceRow> Invoices { get; set; } = new();
        public string? SearchTerm { get; set; }
        public PurchaseInvoiceStatus? StatusFilter { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public int TotalCount { get; set; }
        public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    }

    public class PurchaseInvoiceRow
    {
        public int PurchaseInvoiceId { get; set; }
        public string InvoiceNumber { get; set; } = string.Empty;
        public DateTime InvoiceDate { get; set; }
        public string SupplierName { get; set; } = string.Empty;
        public decimal GrandTotal { get; set; }
        public PurchaseInvoiceStatus Status { get; set; }
    }

    public class PurchaseInvoiceSearchRequest
    {
        public string? SearchTerm { get; set; }
        public PurchaseInvoiceStatus? StatusFilter { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }

    /// <summary>عرض تفاصيل الفاتورة (للاطلاع فقط - Posted / Cancelled، أو Draft للمراجعة السريعة).</summary>
    public class PurchaseInvoiceDetailsViewModel
    {
        public int PurchaseInvoiceId { get; set; }
        public string InvoiceNumber { get; set; } = string.Empty;
        public DateTime InvoiceDate { get; set; }
        public string SupplierName { get; set; } = string.Empty;
        public string? SupplierPhone { get; set; }
        public string? SupplierAddress { get; set; }
        public string? Notes { get; set; }
        public decimal SubTotal { get; set; }
        public decimal SalesTaxPercent { get; set; }
        public decimal SalesTaxAmount { get; set; }
        public decimal GrandTotal { get; set; }
        public PurchaseInvoiceStatus Status { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime CreatedDate { get; set; }
        public string? PostedBy { get; set; }
        public DateTime? PostedDate { get; set; }
        public string? CancelledBy { get; set; }
        public DateTime? CancelledDate { get; set; }
        public List<PurchaseInvoiceDetailRow> Lines { get; set; } = new();
    }

    public class PurchaseInvoiceDetailRow
    {
        public string ProductName { get; set; } = string.Empty;
        public string Unit { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal LineTotal { get; set; }
    }
}