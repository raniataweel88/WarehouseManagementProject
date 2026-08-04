using System.ComponentModel.DataAnnotations;

namespace WarehouseUserManagement.Models
{
    /// <summary>
    /// أرشيف الفواتير القديمة (سكانات/صور فواتير شراء قديمة انتهت فعلياً ولن يُدخل بياناتها بالنظام).
    /// موديول مستقل تماماً عن PurchaseInvoices: لا Status، لا رقم فاتورة مطلوب، لا مورد مطلوب،
    /// لا يؤثر على المخزون أو أي تقرير. الهدف حفظ الملف فقط مع حقول نصية اختيارية تساعد بالبحث لاحقاً.
    /// </summary>
    public class OldInvoiceArchive
    {
        public int OldInvoiceArchiveId { get; set; }

        // مسار الملف على القرص تحت wwwroot/uploads/old-invoices
        [Required]
        [MaxLength(300)]
        public string StoredPath { get; set; } = string.Empty;

        // الاسم الأصلي للملف كما رفعه المستخدم (يُعرض بدل الاسم العشوائي المخزّن)
        [Required]
        [MaxLength(260)]
        public string FileName { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? ContentType { get; set; }

        public long FileSizeBytes { get; set; }

        // حقول اختيارية للبحث فقط - نص حر بدون أي تحقق أو التزام بالتعبئة
        [MaxLength(150)]
        [Display(Name = "اسم المورد (اختياري)")]
        public string? SupplierNameNote { get; set; }

        [Display(Name = "تاريخ الفاتورة تقريباً (اختياري)")]
        public DateTime? InvoiceDateNote { get; set; }

        [MaxLength(500)]
        [Display(Name = "ملاحظات")]
        public string? Notes { get; set; }

        public string? UploadedBy { get; set; }
        public DateTime UploadedDate { get; set; } = DateTime.UtcNow;
    }
}
