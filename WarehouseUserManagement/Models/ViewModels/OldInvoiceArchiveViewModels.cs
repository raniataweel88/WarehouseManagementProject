using System.ComponentModel.DataAnnotations;

namespace WarehouseUserManagement.Models.ViewModels
{
    /// <summary>طلب رفع دفعة من ملفات الفواتير القديمة دفعة واحدة.</summary>
    public class OldInvoiceArchiveUploadViewModel
    {
        [Required(ErrorMessage = "اختر ملف واحد على الأقل")]
        [Display(Name = "الملفات (صور أو PDF)")]
        public List<IFormFile> Files { get; set; } = new();

        // ملاحظات مشتركة اختيارية تُطبّق على كل الملفات المرفوعة بهذه الدفعة (نفس المورد مثلاً)
        [MaxLength(150)]
        [Display(Name = "اسم المورد (اختياري)")]
        public string? SupplierNameNote { get; set; }

        [Display(Name = "تاريخ الفاتورة تقريباً (اختياري)")]
        [DataType(DataType.Date)]
        public DateTime? InvoiceDateNote { get; set; }

        [MaxLength(500)]
        [Display(Name = "ملاحظات")]
        public string? Notes { get; set; }
    }

    public class OldInvoiceArchiveRow
    {
        public int OldInvoiceArchiveId { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string StoredPath { get; set; } = string.Empty;
        public string? ContentType { get; set; }
        public long FileSizeBytes { get; set; }
        public string? SupplierNameNote { get; set; }
        public DateTime? InvoiceDateNote { get; set; }
        public string? Notes { get; set; }
        public string? UploadedBy { get; set; }
        public DateTime UploadedDate { get; set; }

        public bool IsImage => (ContentType ?? "").StartsWith("image/", StringComparison.OrdinalIgnoreCase);
    }

    public class OldInvoiceArchiveSearchRequest
    {
        public string? SearchTerm { get; set; } // يبحث باسم الملف أو اسم المورد أو الملاحظات
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 24;
    }

    public class OldInvoiceArchiveListViewModel
    {
        public List<OldInvoiceArchiveRow> Files { get; set; } = new();
        public string? SearchTerm { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
        public int TotalPages => PageSize <= 0 ? 1 : (int)Math.Ceiling(TotalCount / (double)PageSize);
    }
}
