using System.ComponentModel.DataAnnotations;

namespace WarehouseUserManagement.Models.ViewModels
{
    public class StockTakeFormViewModel
    {
        public int StockTakeId { get; set; }

        [Required(ErrorMessage = "تاريخ الجرد مطلوب")]
        [Display(Name = "تاريخ الجرد")]
        [DataType(DataType.Date)]
        public DateTime StockTakeDate { get; set; } = DateTime.UtcNow.Date;

        [MaxLength(500)]
        [Display(Name = "ملاحظات")]
        public string? Notes { get; set; }

        public byte[]? RowVersion { get; set; }

        public List<StockTakeLineViewModel> Lines { get; set; } = new();

        public List<StockTakeProductOption> ProductOptions { get; set; } = new();
    }

    /// <summary>
    /// سطر جرد. SystemQuantity لا تُملأ من الشاشة أبداً - تُجلب وتُفرض من السيرفر عند الحفظ
    /// (Create/Edit) وتُعاد قراءتها من جديد وقت الاعتماد، بحيث لا يمكن للمستخدم التلاعب بها.
    /// </summary>
    public class StockTakeLineViewModel
    {
        [Required(ErrorMessage = "الصنف مطلوب")]
        [Display(Name = "الصنف")]
        public int ProductId { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "الكمية الفعلية لا يمكن أن تكون سالبة")]
        [Display(Name = "الكمية الفعلية")]
        public decimal ActualQuantity { get; set; }
    }

    public class StockTakeListViewModel
    {
        public List<StockTakeRow> StockTakes { get; set; } = new();
        public string? SearchTerm { get; set; }
        public StockTakeStatus? StatusFilter { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public int TotalCount { get; set; }
        public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    }

    public class StockTakeRow
    {
        public int StockTakeId { get; set; }
        public DateTime StockTakeDate { get; set; }
        public string? Notes { get; set; }
        public StockTakeStatus Status { get; set; }
        public int LinesCount { get; set; }
    }

    public class StockTakeSearchRequest
    {
        public string? SearchTerm { get; set; }
        public StockTakeStatus? StatusFilter { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }

    /// <summary>عرض تفاصيل سند الجرد (Draft للمراجعة أو Posted للاطلاع على أثره النهائي على المخزون).</summary>
    public class StockTakeDetailsViewModel
    {
        public int StockTakeId { get; set; }
        public DateTime StockTakeDate { get; set; }
        public string? Notes { get; set; }
        public StockTakeStatus Status { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime CreatedDate { get; set; }
        public string? PostedBy { get; set; }
        public DateTime? PostedDate { get; set; }
        public List<StockTakeDetailRow> Lines { get; set; } = new();
    }

    public class StockTakeDetailRow
    {
        public string ProductName { get; set; } = string.Empty;
        public string Unit { get; set; } = string.Empty;
        public decimal SystemQuantity { get; set; }
        public decimal ActualQuantity { get; set; }
        public decimal Difference { get; set; }
    }

    /// <summary>عنصر اختيار الصنف داخل سند الجرد، يحمل الكمية الحالية بالنظام للعرض الفوري في الواجهة.</summary>
    public class StockTakeProductOption
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public string Unit { get; set; } = string.Empty;
        public decimal CurrentQuantity { get; set; }
    }
}
