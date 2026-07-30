using System.ComponentModel.DataAnnotations;

namespace WarehouseUserManagement.Models
{
    /// <summary>جدول الصلاحيات - Permissions table (e.g. CREATE_PRODUCT).</summary>
    public class Permission
    {
        public int PermissionId { get; set; }

        [Required]
        [MaxLength(100)]
        public string PermissionName { get; set; } = string.Empty; // e.g. "Add Product"

        [Required]
        [MaxLength(50)]
        public string ModuleName { get; set; } = string.Empty; // e.g. "Products"

        [MaxLength(250)]
        public string? Description { get; set; }

        public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
    }
}
