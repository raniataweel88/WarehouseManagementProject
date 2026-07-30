using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WarehouseUserManagement.Data;
using WarehouseUserManagement.Models.ViewModels;
using WarehouseUserManagement.Services;

namespace WarehouseUserManagement.Controllers
{
    public class AccountController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IPermissionService _permissionService;
        private readonly IConfiguration _config;

        public AccountController(
            AppDbContext context,
            IPasswordHasher passwordHasher,
            IPermissionService permissionService,
             IConfiguration config)
        {
            _context = context;
            _passwordHasher = passwordHasher;
            _permissionService = permissionService;
            _config = config;
        }

        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            if (User.Identity?.IsAuthenticated == true)
                return RedirectToAction("Index", "Home");

            return View(new LoginViewModel { ReturnUrl = returnUrl });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            // Username مفهرس (Unique Index) -> بحث O(log n) بدل مسح الجدول كاملاً
            var user = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Username == model.Username);

            // Login Flow (وثيقة التحليل قسم 12): Validate -> IsActive -> Role -> Permissions -> Session
            if (user is null)
            {
                ModelState.AddModelError(string.Empty, "اسم المستخدم أو كلمة المرور غير صحيحة");
                return View(model);
            }


            if (!user.IsActive)
            {
                ModelState.AddModelError(string.Empty, "الحساب غير مفعل");
                return View(model);
            }

            if (!_passwordHasher.VerifyPassword(model.Password, user.PasswordHash))
            {

                ModelState.AddModelError(string.Empty, "اسم المستخدم أو كلمة المرور غير صحيحة");
                return View(model);
            }


            user.LastLoginDate = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            // تحميل صلاحيات الدور من الكاش (O(1) بعد أول تحميل) وتضمينها كـ Claims
            // بحيث لا يحتاج أي طلب لاحق ضرب قاعدة البيانات للتحقق من الصلاحيات.
            var permissionNames = await _permissionService.GetPermissionNamesForRoleAsync(user.RoleId);

            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                new(ClaimTypes.Name, user.Username),
                new("FullName", user.FullName),
                new(ClaimTypes.Role, user.Role!.RoleName),
                new("RoleId", user.RoleId.ToString())
            };

            if (user.Role.RoleName.Equals("Admin", StringComparison.OrdinalIgnoreCase))
            {
                claims.Add(new Claim("perm", "*"));
            }
            else
            {
                claims.AddRange(permissionNames.Select(p => new Claim("perm", p)));
            }

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            var authProperties = new AuthenticationProperties
            {
                IsPersistent = model.RememberMe,
                ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(_config.GetValue<int?>("AppSettings:SessionTimeoutMinutes") ?? 30)
            };

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, authProperties);

            if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
                return Redirect(model.ReturnUrl);


            if (user.Role.RoleName.Equals("Admin", StringComparison.OrdinalIgnoreCase))
            {
                return RedirectToAction("Index", "Home");
            }

            return RedirectToAction("Dashboard", "Home");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);


            return RedirectToAction("Login");
        }

        [HttpGet]
        public IActionResult AccessDenied() => View();

        // GET: /Account/ChangePassword
        // متاحة لأي مستخدم مسجّل دخول لتغيير كلمة مروره الخاصة به - بدون أي صلاحية إضافية،
        // فقط [Authorize] العادية (وليست [HasPermission] كباقي شاشات الإدارة).
        [HttpGet]
        [Authorize]
        public IActionResult ChangePassword()
        {
            return View(new ChangePasswordViewModel());
        }

        // POST: /Account/ChangePassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdValue, out var userId))
                return View("login");

            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == userId);
            if (user is null)
                return View("login");

            // التحقق من كلمة المرور الحالية قبل السماح بالتغيير - هذا الفرق الجوهري عن
            // إعادة التعيين من شاشة المستخدمين (اللي يقدر عليها الأدمن بدون معرفة كلمة المرور القديمة).
            if (!_passwordHasher.VerifyPassword(model.CurrentPassword, user.PasswordHash))
            {
                ModelState.AddModelError(nameof(model.CurrentPassword), "كلمة المرور الحالية غير صحيحة");
                return View(model);
            }

            user.PasswordHash = _passwordHasher.HashPassword(model.NewPassword);
            user.UpdatedBy = user.Username;
            user.UpdatedDate = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            const string successMessage = "تم تغيير كلمة المرور بنجاح";

            // إذا كان الطلب AJAX (من المودال) نرجّع JSON بدل Redirect،
            // لأن الـ Redirect بيكسر المودال ويطلع الصفحة الجزئية عارية بدون تنسيق.
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new { success = true, message = successMessage });
            }

            TempData["Success"] = successMessage;
            return RedirectToAction("Dashboard", "Home");
        }

    }
}