using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WarehouseUserManagement.Models
{
    public enum IssueStatus
    {
        Draft = 0,
        Posted = 1,
        Cancelled = 2
    }

    /// <summary>
    /// جدول سندات الصرف - Issues (رأس السند).
    /// IssueNumber فريد. السند قابل للتعديل فقط بحالة Draft، وغير قابل للتعديل بعد Posted.
    /// عند الاعتماد (Posted) يتم خصم الكميات من كل صنف وإنشاء حركات مخزون (StockTransactions) سالبة.
    /// عند الإلغاء (Cancelled) لسند معتمد سابقاً، تُعاد الكميات وتُنشأ حركة مخزون عكسية موجبة.
    /// </summary>
    public class Issue
    {
        public int IssueId { get; set; }

        [Required(ErrorMessage = "رقم الصرف مطلوب")]
        [MaxLength(50)]
        [Display(Name = "رقم الصرف")]
        public string IssueNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "تاريخ الصرف مطلوب")]
        [Display(Name = "تاريخ الصرف")]
        public DateTime IssueDate { get; set; } = DateTime.UtcNow.Date;

        [Required(ErrorMessage = "القسم مطلوب")]
        [Display(Name = "القسم المستلم")]
        public int DepartmentId { get; set; }
        public Department? Department { get; set; }

        [Required(ErrorMessage = "اسم المستلم مطلوب")]
        [MinLength(3, ErrorMessage = "اسم المستلم يجب أن لا يقل عن 3 أحرف")]
        [MaxLength(150)]
        [Display(Name = "اسم المستلم")]
        public string ReceiverName { get; set; } = string.Empty;

        [MaxLength(500)]
        [Display(Name = "ملاحظات")]
        public string? Notes { get; set; }

        [Display(Name = "الحالة")]
        public IssueStatus Status { get; set; } = IssueStatus.Draft;

        public string? CreatedBy { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public string? PostedBy { get; set; }
        public DateTime? PostedDate { get; set; }
        public string? CancelledBy { get; set; }
        public DateTime? CancelledDate { get; set; }

        [Timestamp]
        public byte[]? RowVersion { get; set; }

        public List<IssueDetail> Details { get; set; } = new();
    }
}
