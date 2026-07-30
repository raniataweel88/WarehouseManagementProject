using System.ComponentModel.DataAnnotations.Schema;

namespace WarehouseUserManagement.Models
{
    public enum StockTransactionType
    {
        Purchase = 0,        // إدخال كمية نتيجة اعتماد فاتورة شراء
        PurchaseReverse = 1, // إخراج/عكس الكمية نتيجة إلغاء فاتورة شراء معتمدة
        Issue = 2,           // إخراج كمية نتيجة اعتماد سند صرف
        IssueReverse = 3,    // إعادة/عكس الكمية نتيجة إلغاء سند صرف معتمد
        Adjustment = 4       // تسوية كمية نتيجة اعتماد سند جرد (موجبة إن زادت الكمية الفعلية عن كمية النظام، وسالبة إن نقصت)
    }

    /// <summary>
    /// سجل حركات المخزون - يُنشأ تلقائياً عند اعتماد أو إلغاء فاتورة شراء أو سند صرف (ولاحقاً الجرد).
    /// Quantity موجبة عند الإدخال (Purchase) وسالبة عند العكس (PurchaseReverse).
    /// فهرس على ProductId لتسريع كشف حركة أي صنف O(log n) بدل مسح الجدول بالكامل.
    /// </summary>
    public class StockTransaction
    {
        public int StockTransactionId { get; set; }

        public int ProductId { get; set; }
        public Product? Product { get; set; }

        public StockTransactionType TransactionType { get; set; }

        [Column(TypeName = "decimal(18,3)")]
        public decimal Quantity { get; set; }

        [Column(TypeName = "decimal(18,4)")]
        public decimal? UnitCost { get; set; }

        // مصدر الحركة (مثلاً "PurchaseInvoice") ومعرّف المستند المصدر، لتتبع أصل كل حركة
        public string ReferenceType { get; set; } = string.Empty;
        public int ReferenceId { get; set; }

        public DateTime TransactionDate { get; set; } = DateTime.UtcNow;
        public string? CreatedBy { get; set; }
    }
}
