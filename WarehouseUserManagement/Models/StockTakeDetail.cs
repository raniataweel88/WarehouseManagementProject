using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WarehouseUserManagement.Models
{
    /// <summary>
    /// تفاصيل الجرد - سطر واحد لكل صنف تم عدّه.
    /// SystemQuantity تُجلب تلقائياً من Product.CurrentQuantity في السيرفر وقت الحفظ (Create/Edit) - غير
    /// قابلة للتعديل يدوياً من الشاشة أبداً، وتُعاد قراءتها من جديد وقت الاعتماد (Post) لتعكس آخر قيمة فعلية.
    /// Difference = ActualQuantity - SystemQuantity، تُحسب تلقائياً في الـ Controller وتُحدَّث مرة أخيرة
    /// وقت الاعتماد لتعكس الفرق الفعلي المطبَّق على المخزون.
    /// لا يسمح بتكرار نفس الصنف أكثر من مرة داخل نفس سند الجرد (Business Rule).
    /// </summary>
    public class StockTakeDetail
    {
        public int StockTakeDetailId { get; set; }

        public int StockTakeId { get; set; }
        public StockTake? StockTake { get; set; }

        [Required(ErrorMessage = "الصنف مطلوب")]
        [Display(Name = "الصنف")]
        public int ProductId { get; set; }
        public Product? Product { get; set; }

        [Column(TypeName = "decimal(18,3)")]
        [Display(Name = "كمية النظام")]
        public decimal SystemQuantity { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "الكمية الفعلية لا يمكن أن تكون سالبة")]
        [Column(TypeName = "decimal(18,3)")]
        [Display(Name = "الكمية الفعلية")]
        public decimal ActualQuantity { get; set; }

        [Column(TypeName = "decimal(18,3)")]
        [Display(Name = "الفرق")]
        public decimal Difference { get; set; }
    }
}
