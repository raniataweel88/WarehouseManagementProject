using System.ComponentModel.DataAnnotations;

namespace WarehouseUserManagement.Models.ViewModels
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "اسم المستخدم مطلوب")]
        [Display(Name = "اسم المستخدم")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "كلمة المرور مطلوبة")]
        [DataType(DataType.Password)]
        [Display(Name = "كلمة المرور")]
        public string Password { get; set; } = string.Empty;

        public bool RememberMe { get; set; }

        public string? ReturnUrl { get; set; }
    }
}
