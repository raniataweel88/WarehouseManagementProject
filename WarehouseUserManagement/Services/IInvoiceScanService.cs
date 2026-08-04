using WarehouseUserManagement.Models;
using WarehouseUserManagement.Models.ViewModels;

namespace WarehouseUserManagement.Services
{
    public interface IInvoiceScanService
    {
        /// <summary>
        /// يرسل صورة/PDF فاتورة الشراء الورقية إلى Claude (Vision) ويستخرج منها بيانات الفاتورة
        /// والأصناف، ثم يحاول مطابقة كل صنف مستخرج مع كتالوج الأصناف الفعّال بالنظام.
        /// </summary>
        Task<ScannedInvoiceResult> ScanAsync(byte[] fileBytes, string contentType, List<Product> activeProducts, CancellationToken ct = default);
    }

    /// <summary>يُرمى عند فشل الاستخراج (مفتاح API غير مضبوط، خطأ من الـ API، أو استجابة غير قابلة للتحليل).</summary>
    public class InvoiceScanException : Exception
    {
        public InvoiceScanException(string message) : base(message) { }
    }
}
