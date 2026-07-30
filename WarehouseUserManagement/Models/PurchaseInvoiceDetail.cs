using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WarehouseUserManagement.Models
{
    /// <summary>
    /// تفاصيل فاتورة الشراء - سطر واحد لكل صنف ضمن الفاتورة.
    /// LineTotal = Quantity × UnitPrice (تُحسب تلقائياً في الـ Controller).
    /// لا يسمح بتكرار نفس الصنف أكثر من مرة داخل نفس الفاتورة (Business Rule).
    /// </summary>
    public class PurchaseInvoiceDetail
    {
        public int PurchaseInvoiceDetailId { get; set; }

        public int PurchaseInvoiceId { get; set; }
        public PurchaseInvoice? PurchaseInvoice { get; set; }

        [Required(ErrorMessage = "الصنف مطلوب")]
        [Display(Name = "الصنف")]
        public int ProductId { get; set; }
        public Product? Product { get; set; }

        [Range(0.001, double.MaxValue, ErrorMessage = "الكمية يجب أن تكون أكبر من صفر")]
        [Column(TypeName = "decimal(18,3)")]
        [Display(Name = "الكمية")]
        public decimal Quantity { get; set; }

        [Range(0.01, double.MaxValue, ErrorMessage = "سعر الوحدة يجب أن يكون أكبر من صفر")]
        [Column(TypeName = "decimal(18,4)")]
        [Display(Name = "سعر الوحدة")]
        public decimal UnitPrice { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "إجمالي السطر")]
        public decimal LineTotal { get; set; }
    }
}
