namespace WarehouseUserManagement.Models.ViewModels
{
    /// <summary>
    /// نتيجة استخراج بيانات فاتورة شراء ورقية (صورة/PDF) بواسطة الذكاء الاصطناعي (Claude Vision).
    /// الحقول كلها اختيارية لأن الاستخراج قد لا يتمكن من قراءة بعضها بوضوح من المستند الممسوح.
    /// </summary>
    public class ScannedInvoiceResult
    {
        public string? InvoiceNumber { get; set; }
        public DateTime? InvoiceDate { get; set; }
        public string? SupplierName { get; set; }
        public string? SupplierPhone { get; set; }
        public string? SupplierAddress { get; set; }
        public string? Notes { get; set; }
        public List<ScannedInvoiceItem> Items { get; set; } = new();
    }

    /// <summary>
    /// سطر صنف مستخرج من الفاتورة. إذا أمكن مطابقته مع صنف موجود فعلياً بالنظام (بالباركود أو الاسم)
    /// تُملأ ProductId، وإلا يبقى المستخدم يختار الصنف يدوياً من القائمة بالاعتماد على RawName.
    /// </summary>
    public class ScannedInvoiceItem
    {
        public string RawName { get; set; } = string.Empty;
        public string? Barcode { get; set; }
        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public int? ProductId { get; set; }
        public bool Matched => ProductId.HasValue;
    }
}
