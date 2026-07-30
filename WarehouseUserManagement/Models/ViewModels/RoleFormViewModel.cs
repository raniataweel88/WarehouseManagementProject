using System.ComponentModel.DataAnnotations;

namespace WarehouseUserManagement.Models.ViewModels
{
    public class RoleFormViewModel
    {
        public int RoleId { get; set; }

        [Required(ErrorMessage = "اسم الدور مطلوب")]
        [MaxLength(50)]
        [Display(Name = "اسم الدور")]
        public string RoleName { get; set; } = string.Empty;

        [MaxLength(250)]
        [Display(Name = "الوصف")]
        public string? Description { get; set; }

        [Display(Name = "مفعل")]
        public bool IsActive { get; set; } = true;

        // مجموعة معرفات الصلاحيات المختارة -> HashSet لسرعة الفحص O(1) عند البناء والحفظ
        public List<int> SelectedPermissionIds { get; set; } = new();

        public List<PermissionGroup> PermissionGroups { get; set; } = new();
    }

    public class PermissionGroup
    {
        public string ModuleName { get; set; } = string.Empty;
        public List<PermissionOption> Permissions { get; set; } = new();
    }

    public class PermissionOption
    {
        public int PermissionId { get; set; }
        public string PermissionName { get; set; } = string.Empty;
        public bool IsSelected { get; set; }
    }

    public class RoleListItem
    {
        public int RoleId { get; set; }
        public string RoleName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsActive { get; set; }
        public int UsersCount { get; set; }
        public int PermissionsCount { get; set; }
    }
}
