using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WarehouseUserManagement.Models
{
    /// <summary>
    /// جدول الأصناف - Products table.
    /// ProductName فريد ضمن نفس التصنيف (Compound Unique Index على CategoryId + ProductName).
    /// Barcode فريد إن وُجد (Nullable Unique Index).
    /// CurrentQuantity و AverageCost لا يُعدّلان يدوياً - يتم تحديثهما فقط من خلال حركات المخزون
    /// (فواتير الشراء / الصرف / الجرد / التسويات).
    /// </summary>
    public class Product
    {
        public int ProductId { get; set; }

        [Required(ErrorMessage = "التصنيف مطلوب")]
        [Display(Name = "التصنيف")]
        public int CategoryId { get; set; }

        public Category? Category { get; set; }

        [Required(ErrorMessage = "اسم الصنف مطلوب")]
        [MinLength(2, ErrorMessage = "اسم الصنف يجب أن لا يقل عن 2 حرف")]
        [MaxLength(150)]
        [Display(Name = "اسم الصنف")]
        public string ProductName { get; set; } = string.Empty;

        [MaxLength(64)]
        [Display(Name = "الباركود")]
        public string? Barcode { get; set; }

        [Required(ErrorMessage = "وحدة القياس مطلوبة")]
        [MaxLength(30)]
        [Display(Name = "الوحدة")]
        public string Unit { get; set; } = string.Empty;

        // تُحدَّث فقط عبر حركات المخزون (Purchase / Issue / StockTake / Adjustment) - لا تُعدَّل يدوياً من الشاشة.
        [Column(TypeName = "decimal(18,3)")]
        [Display(Name = "الكمية الحالية")]
        public decimal CurrentQuantity { get; set; } = 0;

        // متوسط التكلفة المرجّح - يُحدَّث تلقائياً عند كل عملية شراء (Weighted Average Cost).
        [Column(TypeName = "decimal(18,4)")]
        [Display(Name = "متوسط التكلفة")]
        public decimal AverageCost { get; set; } = 0;

        [Range(0, double.MaxValue, ErrorMessage = "الحد الأدنى للكمية لا يمكن أن يكون سالباً")]
        [Column(TypeName = "decimal(18,3)")]
        [Display(Name = "الحد الأدنى للكمية")]
        public decimal MinimumQuantity { get; set; } = 0;

        [Display(Name = "الحالة")]
        public bool IsActive { get; set; } = true;

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public string? CreatedBy { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public string? UpdatedBy { get; set; }

        // Concurrency token -> يمنع Lost Update عند تعديل نفس الصنف من جلستين بنفس الوقت
        [Timestamp]
        public byte[]? RowVersion { get; set; }

        [NotMapped]
        [Display(Name = "منخفض المخزون")]
        public bool IsLowStock => CurrentQuantity <= MinimumQuantity;

        public bool IsDeleted { get; internal set; }
    }
}
