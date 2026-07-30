using System.ComponentModel.DataAnnotations;

namespace WarehouseUserManagement.Models.ViewModels
{
    public class ProductFormViewModel
    {
        public int ProductId { get; set; }

        [Required(ErrorMessage = "التصنيف مطلوب")]
        [Display(Name = "التصنيف")]
        public int CategoryId { get; set; }

        [Required(ErrorMessage = "اسم الصنف مطلوب")]
        [MinLength(2, ErrorMessage = "الحد الأدنى حرفين")]
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

        [Range(0, double.MaxValue, ErrorMessage = "الحد الأدنى للكمية لا يمكن أن يكون سالباً")]
        [Display(Name = "الحد الأدنى للكمية")]
        public decimal MinimumQuantity { get; set; } = 0;

        [Display(Name = "الصنف مفعل")]
        public bool IsActive { get; set; } = true;

        public byte[]? RowVersion { get; set; }

        /// <summary>قائمة التصنيفات النشطة لعرضها في القائمة المنسدلة.</summary>
        public List<CategoryOption> CategoryOptions { get; set; } = new();
        public int AverageCost { get;   set; }
        public int CurrentQuantity { get;   set; }
    }

    public class CategoryOption
    {
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
    }

    /// <summary>
    /// نتيجة بحث/عرض الأصناف. Paging على مستوى SQL Server (OFFSET/FETCH).
    /// </summary>
    public class ProductListViewModel
    {
        public List<ProductRow> Products { get; set; } = new();
        public string? SearchTerm { get; set; }
        public int? CategoryFilter { get; set; }
        public bool? StatusFilter { get; set; }
        public bool LowStockOnly { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public int TotalCount { get; set; }
        public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
        public List<CategoryOption> CategoryOptions { get; set; } = new();
    }

    public class ProductRow
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public string? Barcode { get; set; }
        public string Unit { get; set; } = string.Empty;
        public decimal CurrentQuantity { get; set; }
        public decimal AverageCost { get; set; }
        public decimal MinimumQuantity { get; set; }
        public bool IsActive { get; set; }
        public bool IsLowStock => CurrentQuantity <= MinimumQuantity;
    }

    public class ProductSearchRequest
    {
        public string? SearchTerm { get; set; }
        public int? CategoryFilter { get; set; }
        public bool? StatusFilter { get; set; }
        public bool LowStockOnly { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }

    /// <summary>عنصر مبسّط يُستخدم في قوائم اختيار الصنف (مثلاً داخل فاتورة الشراء).</summary>
    public class ProductOption
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public string Unit { get; set; } = string.Empty;
        public decimal AverageCost { get; set; }
    }
}
