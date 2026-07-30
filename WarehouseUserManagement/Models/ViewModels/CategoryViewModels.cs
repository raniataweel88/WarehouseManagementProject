 
    using System.ComponentModel.DataAnnotations;

    namespace WarehouseUserManagement.Models.ViewModels
    {
        public class CategoryFormViewModel
        {
            public int CategoryId { get; set; }

            [Required(ErrorMessage = "اسم التصنيف مطلوب")]
            [MinLength(2, ErrorMessage = "الحد الأدنى حرفين")]
            [MaxLength(100)]
            [Display(Name = "اسم التصنيف")]
            public string CategoryName { get; set; } = string.Empty;

            [MaxLength(500, ErrorMessage = "الوصف لا يتجاوز 500 حرف")]
            [Display(Name = "الوصف")]
            public string? Description { get; set; }

            [Display(Name = "التصنيف مفعل")]
            public bool IsActive { get; set; } = true;

            public byte[]? RowVersion { get; set; }
        }

        /// <summary>
        /// نتيجة بحث/عرض التصنيفات. Paging على مستوى SQL Server (OFFSET/FETCH)
        /// بدل تحميل كل الجدول للذاكرة، بالاستناد للفهرس على CategoryName.
        /// </summary>
        public class CategoryListViewModel
        {
            public List<CategoryRow> Categories { get; set; } = new();
            public string? SearchTerm { get; set; }
            public bool? StatusFilter { get; set; }
            public int PageNumber { get; set; } = 1;
            public int PageSize { get; set; } = 10;
            public int TotalCount { get; set; }
            public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
        }

        public class CategoryRow
        {
            public int CategoryId { get; set; }
            public string CategoryName { get; set; } = string.Empty;
            public string? Description { get; set; }
            public bool IsActive { get; set; }
        }

        public class CategorySearchRequest
        {
            public string? SearchTerm { get; set; }
            public bool? StatusFilter { get; set; }
            public int PageNumber { get; set; } = 1;
            public int PageSize { get; set; } = 10;
        }
    }
 