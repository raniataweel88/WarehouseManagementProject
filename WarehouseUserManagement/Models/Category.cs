using System.ComponentModel.DataAnnotations;

namespace WarehouseUserManagement.Models
{
    /// <summary>
    /// جدول التصنيفات - Categories table (تصنيف الأصناف في المستودع).
    /// CategoryName has a unique index (see AppDbContext) -> O(log n) lookup بدل O(n) Scan.
    /// One Category -> Many Products (سيتم ربطها لاحقاً عند بناء موديول الأصناف).
    /// </summary>
    public class Category
    {
        public int CategoryId { get; set; }

        [Required(ErrorMessage = "اسم التصنيف مطلوب")]
        [MinLength(2, ErrorMessage = "اسم التصنيف يجب أن لا يقل عن 2 حرف")]
        [MaxLength(100)]
        [Display(Name = "اسم التصنيف")]
        public string CategoryName { get; set; } = string.Empty;

        [Display(Name = "الحالة")]
        public bool IsActive { get; set; } = true;

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public string? CreatedBy { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public string? UpdatedBy { get; set; }  // Soft Delete
        public bool IsDeleted { get; set; } = false;
        public string? DeletedBy { get; set; }
        public DateTime? DeletedDate { get; set; }
        // Concurrency token -> يمنع Lost Update عند تعديل نفس التصنيف من جلستين بنفس الوقت
        [Timestamp]
        public byte[]? RowVersion { get; set; }
    }
}