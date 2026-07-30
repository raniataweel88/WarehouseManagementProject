using System.ComponentModel.DataAnnotations;

namespace WarehouseUserManagement.Models
{
    /// <summary>
    /// جدول المستخدمين - Users table.
    /// Username has a unique index (see AppDbContext) -> O(log n) lookup on login/search
    /// instead of O(n) table scan.
    /// </summary>
    public class User
    {
        public int UserId { get; set; }

        [Required(ErrorMessage = "الاسم الكامل مطلوب")]
        [MinLength(3, ErrorMessage = "الاسم الكامل يجب أن لا يقل عن 3 أحرف")]
        [MaxLength(150)]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "اسم المستخدم مطلوب")]
        [MaxLength(50)]
        public string Username { get; set; } = string.Empty;

        // يخزن الهاش فقط، لا يخزن كلمة المرور مباشرة (Security Requirement #9)
        [Required]
        public string PasswordHash { get; set; } = string.Empty;

        [Required(ErrorMessage = "يجب اختيار الدور (Role)")]
        public int RoleId { get; set; }
        public Role? Role { get; set; }

        [MaxLength(150)]
        [EmailAddress(ErrorMessage = "صيغة البريد الإلكتروني غير صحيحة")]
        public string? Email { get; set; }

        [MaxLength(20)]
        [RegularExpression(@"^\d*$", ErrorMessage = "رقم الهاتف يقبل أرقام فقط")]
        public string? Phone { get; set; }

        public bool IsActive { get; set; } = true;

        // Locked بعد محاولات دخول خاطئة متكررة (Business Rule / User Status #13)
        public bool IsLocked { get; set; } = false;
        public int FailedLoginAttempts { get; set; } = 0;

        public DateTime? LastLoginDate { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public string? CreatedBy { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public string? UpdatedBy { get; set; }

        // Concurrency token -> يمنع Lost Update عند تعديل نفس المستخدم من جلستين بنفس الوقت
        [Timestamp]
        public byte[]? RowVersion { get; set; }
    }

    public enum UserStatus
    {
        Active,
        Inactive,
        Locked
    }
}
