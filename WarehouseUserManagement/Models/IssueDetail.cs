using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WarehouseUserManagement.Models
{
    /// <summary>
    /// تفاصيل سند الصرف - سطر واحد لكل صنف تم صرفه.
    /// لا يسمح بتكرار نفس الصنف أكثر من مرة داخل نفس السند (Business Rule)، ولا بصرف كمية
    /// أكبر من الكمية المتوفرة في المخزون (CurrentQuantity) وقت الاعتماد.
    /// </summary>
    public class IssueDetail
    {
        public int IssueDetailId { get; set; }

        public int IssueId { get; set; }
        public Issue? Issue { get; set; }

        [Required(ErrorMessage = "الصنف مطلوب")]
        [Display(Name = "الصنف")]
        public int ProductId { get; set; }
        public Product? Product { get; set; }

        [Range(0.001, double.MaxValue, ErrorMessage = "الكمية يجب أن تكون أكبر من صفر")]
        [Column(TypeName = "decimal(18,3)")]
        [Display(Name = "الكمية")]
        public decimal Quantity { get; set; }
    }
}
