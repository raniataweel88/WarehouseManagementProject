using System.ComponentModel.DataAnnotations;

namespace WarehouseUserManagement.Models
{
    /// <summary>جدول الأدوار - Roles table. One Role -> Many Users.</summary>
    public class Role
    {
        public int RoleId { get; set; }

        [Required(ErrorMessage = "اسم الدور مطلوب")]
        [MaxLength(50)]
        public string RoleName { get; set; } = string.Empty;

        [MaxLength(250)]
        public string? Description { get; set; }

        public bool IsActive { get; set; } = true;

        public ICollection<User> Users { get; set; } = new List<User>();
        public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
    }
}
