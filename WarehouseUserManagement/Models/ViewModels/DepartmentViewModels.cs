using System.ComponentModel.DataAnnotations;

namespace WarehouseUserManagement.Models.ViewModels
{
    public class DepartmentFormViewModel
    {
        public int DepartmentId { get; set; }

        [Required(ErrorMessage = "اسم القسم مطلوب")]
        [MinLength(2, ErrorMessage = "الحد الأدنى حرفين")]
        [MaxLength(100)]
        [Display(Name = "اسم القسم")]
        public string DepartmentName { get; set; } = string.Empty;

        [MaxLength(500, ErrorMessage = "الوصف لا يتجاوز 500 حرف")]
        [Display(Name = "الوصف")]
        public string? Description { get; set; }

        [Display(Name = "القسم مفعل")]
        public bool IsActive { get; set; } = true;

        public byte[]? RowVersion { get; set; }
    }

    /// <summary>
    /// نتيجة بحث/عرض الأقسام. Paging على مستوى SQL Server (OFFSET/FETCH)
    /// بدل تحميل كل الجدول للذاكرة، بالاستناد للفهرس على DepartmentName.
    /// </summary>
    public class DepartmentListViewModel
    {
        public List<DepartmentRow> Departments { get; set; } = new();
        public string? SearchTerm { get; set; }
        public bool? StatusFilter { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public int TotalCount { get; set; }
        public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    }

    public class DepartmentRow
    {
        public int DepartmentId { get; set; }
        public string DepartmentName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsActive { get; set; }
    }

    public class DepartmentSearchRequest
    {
        public string? SearchTerm { get; set; }
        public bool? StatusFilter { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }
}
