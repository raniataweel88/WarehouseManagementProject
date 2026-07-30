using System.ComponentModel.DataAnnotations;

namespace WarehouseUserManagement.Models.ViewModels
{
    public class UserFormViewModel
    {
        public int UserId { get; set; }

        [Required(ErrorMessage = "الاسم الكامل مطلوب")]
        [MinLength(3, ErrorMessage = "الحد الأدنى 3 أحرف")]
        [RegularExpression(@".*[^\d].*", ErrorMessage = "لا يقبل أرقام فقط")]
        [Display(Name = "الاسم الكامل")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "اسم المستخدم مطلوب")]
        [MaxLength(50)]
        [Display(Name = "اسم المستخدم")]
        public string Username { get; set; } = string.Empty;

        // مطلوبة فقط عند الإنشاء - عند التعديل تترك فارغة لعدم تغيير كلمة المرور
        [DataType(DataType.Password)]
        [Display(Name = "كلمة المرور")]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^a-zA-Z0-9]).{8,}$",
            ErrorMessage = "كلمة المرور يجب أن تحتوي 8 أحرف على الأقل، حرف كبير، حرف صغير، رقم، ورمز خاص")]
        public string? Password { get; set; }

        [Required(ErrorMessage = "يجب اختيار الدور")]
        [Display(Name = "الدور")]
        public int RoleId { get; set; }

        [EmailAddress(ErrorMessage = "صيغة البريد الإلكتروني غير صحيحة")]
        [Display(Name = "البريد الإلكتروني")]
        public string? Email { get; set; }

        [RegularExpression(@"^\d*$", ErrorMessage = "رقم الهاتف يقبل أرقام فقط")]
        [Display(Name = "رقم الهاتف")]
        public string? Phone { get; set; }

        [Display(Name = "الحساب مفعل")]
        public bool IsActive { get; set; } = true;

        public byte[]? RowVersion { get; set; }

        public List<RoleOption> AvailableRoles { get; set; } = new();
    }

    public class RoleOption
    {
        public int RoleId { get; set; }
        public string RoleName { get; set; } = string.Empty;
    }
}
