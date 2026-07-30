using System.ComponentModel.DataAnnotations;

namespace WarehouseUserManagement.Models.ViewModels
{
    public class ResetPasswordViewModel
    {
        public int UserId { get; set; }
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "كلمة المرور الجديدة مطلوبة")]
        [DataType(DataType.Password)]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^a-zA-Z0-9]).{8,}$",
            ErrorMessage = "كلمة المرور يجب أن تحتوي 8 أحرف على الأقل، حرف كبير، حرف صغير، رقم، ورمز خاص")]
        [Display(Name = "كلمة المرور الجديدة")]
        public string NewPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "تأكيد كلمة المرور مطلوب")]
        [DataType(DataType.Password)]
        [Compare(nameof(NewPassword), ErrorMessage = "كلمتا المرور غير متطابقتين")]
        [Display(Name = "تأكيد كلمة المرور")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    /// <summary>
    /// يستخدمها أي مستخدم مسجّل دخول لتغيير كلمة مروره الخاصة بنفسه (بدون أي صلاحية خاصة،
    /// فقط كونه مسجّل دخول). يختلف عن ResetPasswordViewModel بأنه يتطلب إدخال كلمة المرور
    /// الحالية أولاً للتحقق من الهوية، بينما إعادة التعيين من شاشة المستخدمين (للأدمن) لا تتطلبها.
    /// </summary>
    public class ChangePasswordViewModel
    {
        [Required(ErrorMessage = "كلمة المرور الحالية مطلوبة")]
        [DataType(DataType.Password)]
        [Display(Name = "كلمة المرور الحالية")]
        public string CurrentPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "كلمة المرور الجديدة مطلوبة")]
        [DataType(DataType.Password)]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^a-zA-Z0-9]).{8,}$",
            ErrorMessage = "كلمة المرور يجب أن تحتوي 8 أحرف على الأقل، حرف كبير، حرف صغير، رقم، ورمز خاص")]
        [Display(Name = "كلمة المرور الجديدة")]
        public string NewPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "تأكيد كلمة المرور مطلوب")]
        [DataType(DataType.Password)]
        [Compare(nameof(NewPassword), ErrorMessage = "كلمتا المرور غير متطابقتين")]
        [Display(Name = "تأكيد كلمة المرور الجديدة")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    public class UserSearchRequest
    {
        public string? SearchTerm { get; set; }
        public string? RoleFilter { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }
}
