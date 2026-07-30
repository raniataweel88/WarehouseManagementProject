using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WarehouseUserManagement.Models
{
    public enum PurchaseInvoiceStatus
    {
        Draft = 0,
        Posted = 1,
        Cancelled = 2
    }

    /// <summary>
    /// جدول فواتير الشراء - Purchase Invoices (رأس الفاتورة).
    /// InvoiceNumber فريد. الفاتورة قابلة للتعديل فقط بحالة Draft، وتصبح غير قابلة للتعديل بعد Posted.
    /// عند الاعتماد (Posted) يتم تحديث كمية ومتوسط تكلفة كل صنف وإنشاء حركات مخزون (StockTransactions).
    /// عند الإلغاء (Cancelled) لفاتورة معتمدة سابقاً، تُعكس الكميات وتُنشأ حركة مخزون عكسية.
    /// </summary>
    public class PurchaseInvoice
    {
        public int PurchaseInvoiceId { get; set; }

        [Required(ErrorMessage = "رقم الفاتورة مطلوب")]
        [MaxLength(50)]
        [Display(Name = "رقم الفاتورة")]
        public string InvoiceNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "تاريخ الفاتورة مطلوب")]
        [Display(Name = "تاريخ الفاتورة")]
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

        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "المجموع الفرعي")]
        public decimal SubTotal { get; set; }

        [Range(0, 100, ErrorMessage = "نسبة الضريبة يجب أن تكون بين 0 و 100")]
        [Column(TypeName = "decimal(5,2)")]
        [Display(Name = "نسبة الضريبة %")]
        public decimal SalesTaxPercent { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "قيمة الضريبة")]
        public decimal SalesTaxAmount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "الإجمالي الكلي")]
        public decimal GrandTotal { get; set; }

        [Display(Name = "الحالة")]
        public PurchaseInvoiceStatus Status { get; set; } = PurchaseInvoiceStatus.Draft;

        public string? CreatedBy { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public string? PostedBy { get; set; }
        public DateTime? PostedDate { get; set; }
        public string? CancelledBy { get; set; }
        public DateTime? CancelledDate { get; set; }

        [Timestamp]
        public byte[]? RowVersion { get; set; }

        public List<PurchaseInvoiceDetail> Details { get; set; } = new();
    }
}
