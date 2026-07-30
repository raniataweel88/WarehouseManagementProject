using System.ComponentModel.DataAnnotations;

namespace WarehouseUserManagement.Models
{
    public enum StockTakeStatus
    {
        Draft = 0,
        Posted = 1
    }

    /// <summary>
    /// جدول الجرد - StockTake (رأس السند).
    /// السند قابل للتعديل فقط بحالة Draft (لا يؤثر على المخزون بعد).
    /// عند الاعتماد (Posted) يُعاد جلب الكمية الحالية الفعلية لكل صنف (وقت الاعتماد نفسه، وليس وقت
    /// إنشاء المسودة) لضمان دقة النتيجة حتى لو تغيّر المخزون بحركة أخرى بين الاثنين، ثم يُنشأ Adjustment
    /// Transaction بفرق = (الفعلي - الكمية الحالية وقت الاعتماد)، ويُحدَّث CurrentQuantity للصنف ليصبح
    /// مساوياً تماماً للكمية الفعلية المعدودة. لا يوجد إلغاء لسند الجرد بتصميم مقصود (بخلاف الصادر)،
    /// لأن الجرد اعتماد لواقع فعلي وليس التزاماً قابلاً للتراجع عنه.
    /// </summary>
    public class StockTake
    {
        public int StockTakeId { get; set; }

        [Required(ErrorMessage = "تاريخ الجرد مطلوب")]
        [Display(Name = "تاريخ الجرد")]
        public DateTime StockTakeDate { get; set; } = DateTime.UtcNow.Date;

        [MaxLength(500)]
        [Display(Name = "ملاحظات")]
        public string? Notes { get; set; }

        [Display(Name = "الحالة")]
        public StockTakeStatus Status { get; set; } = StockTakeStatus.Draft;

        public string? CreatedBy { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public string? PostedBy { get; set; }
        public DateTime? PostedDate { get; set; }

        [Timestamp]
        public byte[]? RowVersion { get; set; }

        public List<StockTakeDetail> Details { get; set; } = new();
    }
}
