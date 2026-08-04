using System.ComponentModel.DataAnnotations;

namespace WarehouseUserManagement.Models.ViewModels
{
    public class IssueFormViewModel
    {
        public int IssueId { get; set; }

        [Required(ErrorMessage = "رقم الصرف مطلوب")]
        [MaxLength(50)]
        [Display(Name = "رقم الصرف")]
        public string IssueNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "تاريخ الصرف مطلوب")]
        [Display(Name = "تاريخ الصرف")]
        [DataType(DataType.Date)]
        public DateTime IssueDate { get; set; } = DateTime.UtcNow.Date;

        [Required(ErrorMessage = "القسم مطلوب")]
        [Display(Name = "القسم المستلم")]
        public int DepartmentId { get; set; }

        [Required(ErrorMessage = "اسم المستلم مطلوب")]
        [MinLength(3, ErrorMessage = "اسم المستلم يجب أن لا يقل عن 3 أحرف")]
        [MaxLength(150)]
        [Display(Name = "اسم المستلم")]
        public string ReceiverName { get; set; } = string.Empty;

        [MaxLength(500)]
        [Display(Name = "ملاحظات")]
        public string? Notes { get; set; }

        public byte[]? RowVersion { get; set; }

        public List<IssueLineViewModel> Lines { get; set; } = new();

        public List<DepartmentOption> DepartmentOptions { get; set; } = new();
        public List<IssueProductOption> ProductOptions { get; set; } = new();
    }

    public class IssueLineViewModel
    {
        [Required(ErrorMessage = "الصنف مطلوب")]
        [Display(Name = "الصنف")]
        public int ProductId { get; set; }

        [Range(0.001, double.MaxValue, ErrorMessage = "الكمية يجب أن تكون أكبر من صفر")]
        [Display(Name = "الكمية")]
        public decimal Quantity { get; set; }
    }

    public class IssueListViewModel
    {
        public List<IssueRow> Issues { get; set; } = new();
        public string? SearchTerm { get; set; }
        public int? DepartmentFilter { get; set; }
        public IssueStatus? StatusFilter { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public int TotalCount { get; set; }
        public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
        public List<DepartmentOption> DepartmentOptions { get; set; } = new();
    }

    public class IssueRow
    {
        public int IssueId { get; set; }
        public string IssueNumber { get; set; } = string.Empty;
        public DateTime IssueDate { get; set; }
        public string DepartmentName { get; set; } = string.Empty;
        public string ReceiverName { get; set; } = string.Empty;
        public IssueStatus Status { get; set; }
    }

    public class IssueSearchRequest
    {
        public string? SearchTerm { get; set; }
        public int? DepartmentFilter { get; set; }
        public IssueStatus? StatusFilter { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }

    /// <summary>عرض تفاصيل سند الصرف (للاطلاع - Posted / Cancelled، أو Draft للمراجعة السريعة).</summary>
    public class IssueDetailsViewModel
    {
        public int IssueId { get; set; }
        public string IssueNumber { get; set; } = string.Empty;
        public DateTime IssueDate { get; set; }
        public string DepartmentName { get; set; } = string.Empty;
        public string ReceiverName { get; set; } = string.Empty;
        public string? Notes { get; set; }
        public IssueStatus Status { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime CreatedDate { get; set; }
        public string? PostedBy { get; set; }
        public DateTime? PostedDate { get; set; }
        public string? CancelledBy { get; set; }
        public DateTime? CancelledDate { get; set; }
        public List<IssueDetailRow> Lines { get; set; } = new();
    }

    public class IssueDetailRow
    {
        public string ProductName { get; set; } = string.Empty;
        public string Unit { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
    }

    public class DepartmentOption
    {
        public int DepartmentId { get; set; }
        public string DepartmentName { get; set; } = string.Empty;
    }

    /// <summary>عنصر اختيار الصنف داخل سند الصرف، يحمل الكمية الحالية للتحقق الفوري في الواجهة.</summary>
    public class IssueProductOption
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public string Unit { get; set; } = string.Empty;
        public decimal CurrentQuantity { get; set; }
        public string? Barcode { get; set; }
    }
}
