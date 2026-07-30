using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WarehouseUserManagement.Authorization;
using WarehouseUserManagement.Data;
using WarehouseUserManagement.Models;
using WarehouseUserManagement.Models.ViewModels;
using WarehouseUserManagement.Services;

namespace WarehouseUserManagement.Controllers
{
    public class UsersController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IPasswordHasher _passwordHasher;

        private readonly ICurrentUserService _currentUser;

        public UsersController(AppDbContext context, IPasswordHasher passwordHasher,
          ICurrentUserService currentUser)
        {
            _context = context;
            _passwordHasher = passwordHasher;
            _currentUser = currentUser;
        }

        // GET: /Users?searchTerm=&roleFilter=&pageNumber=1
        [HasPermission("View Users")]
        public async Task<IActionResult> Index(UserSearchRequest request)
        {
            var query = _context.Users.AsNoTracking().Include(u => u.Role).AsQueryable();

            if (!string.IsNullOrWhiteSpace(request.SearchTerm))
            {
                var term = request.SearchTerm.Trim();
                // StartsWith يتيح لـ SQL Server استخدام الفهرس (Index Seek O(log n))
                // بعكس Contains (%..%) الذي يجبر على Full Scan O(n).
                query = query.Where(u => u.Username.StartsWith(term) || u.FullName.StartsWith(term));
            }

            if (!string.IsNullOrWhiteSpace(request.RoleFilter))
            {
                query = query.Where(u => u.Role!.RoleName == request.RoleFilter);
            }

            var totalCount = await query.CountAsync();

            var pageSize = request.PageSize is > 0 and <= 100 ? request.PageSize : 10;
            var pageNumber = request.PageNumber < 1 ? 1 : request.PageNumber;

            // Paging على مستوى قاعدة البيانات (OFFSET/FETCH) -> يجلب فقط صفحة واحدة، وليس الجدول كاملاً
            var users = await query
                .OrderBy(u => u.Username) // ترتيب حسب عمود مفهرس
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(u => new UserRow
                {
                    UserId = u.UserId,
                    FullName = u.FullName,
                    Username = u.Username,
                    RoleName = u.Role!.RoleName,
                    Email = u.Email,
                    IsActive = u.IsActive,
                    IsLocked = u.IsLocked,
                    LastLoginDate = u.LastLoginDate
                })
                .ToListAsync();

            var vm = new UserListViewModel
            {
                Users = users,
                SearchTerm = request.SearchTerm,
                RoleFilter = request.RoleFilter,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalCount = totalCount
            };

            vm.AvailableRoles = await GetRoleOptionsAsync();
            return View(vm);
        }

        [HasPermission("View Users")]
        public async Task<IActionResult> Details(int id)
        {
            var user = await _context.Users.AsNoTracking()
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.UserId == id);

            if (user is null) return NotFound();
            return View(user);
        }



        [HttpPost]
        [ValidateAntiForgeryToken]
        [HasPermission("Add User")]
        public async Task<IActionResult> Create(UserFormViewModel model)
        {
            if (string.IsNullOrWhiteSpace(model.Password))
                ModelState.AddModelError(nameof(model.Password),
                    "كلمة المرور مطلوبة عند إنشاء مستخدم جديد");


            if (await _context.Users.AnyAsync(u => u.Username == model.Username))
                ModelState.AddModelError(nameof(model.Username),
                    "Username already exists");


            if (!ModelState.IsValid)
            {
                TempData["Error"] = "يرجى التأكد من البيانات المدخلة";
                return RedirectToAction(nameof(Index));
            }


            var user = new User
            {
                FullName = model.FullName,
                Username = model.Username,
                PasswordHash = _passwordHasher.HashPassword(model.Password!),
                RoleId = model.RoleId,
                Email = model.Email,
                Phone = model.Phone,
                IsActive = model.IsActive,
                CreatedBy = _currentUser.Username,
                CreatedDate = DateTime.UtcNow
            };


            _context.Users.Add(user);
            await _context.SaveChangesAsync();


            TempData["Success"] = "تم إنشاء المستخدم بنجاح";

            return RedirectToAction(nameof(Index));
        }
        [HasPermission("Edit User")]
        public async Task<IActionResult> Edit(int id)
        {
            var user = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.UserId == id);
            if (user is null) return NotFound();

            var vm = new UserFormViewModel
            {
                UserId = user.UserId,
                FullName = user.FullName,
                Username = user.Username,
                RoleId = user.RoleId,
                Email = user.Email,
                Phone = user.Phone,
                IsActive = user.IsActive,
                RowVersion = user.RowVersion,
                AvailableRoles = await GetRoleOptionsAsync()
            };
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [HasPermission("Edit User")]
        public async Task<IActionResult> Edit(int id, UserFormViewModel model)
        {
            if (id != model.UserId) return NotFound();

            // Username Validation: Unique (باستثناء نفس المستخدم)
            if (await _context.Users.AnyAsync(u => u.Username == model.Username && u.UserId != id))
                ModelState.AddModelError(nameof(model.Username), "Username already exists");

            if (!ModelState.IsValid)
            {
                model.AvailableRoles = await GetRoleOptionsAsync();
                return View(model);
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == id);
            if (user is null) return NotFound();

            if (model.RowVersion is not null)
                _context.Entry(user).Property(u => u.RowVersion).OriginalValue = model.RowVersion;

            var oldSnapshot = $"{user.FullName}/{user.RoleId}/{user.IsActive}";

            user.FullName = model.FullName;
            user.Username = model.Username;
            user.RoleId = model.RoleId;
            user.Email = model.Email;
            user.Phone = model.Phone;
            user.IsActive = model.IsActive;
            user.UpdatedBy = _currentUser.Username;
            user.UpdatedDate = DateTime.UtcNow;

            // تغيير كلمة المرور اختياري عند التعديل
            if (!string.IsNullOrWhiteSpace(model.Password))
                user.PasswordHash = _passwordHasher.HashPassword(model.Password);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                ModelState.AddModelError(string.Empty, "تم تعديل هذا المستخدم من جلسة أخرى، الرجاء إعادة تحميل الصفحة");
                model.AvailableRoles = await GetRoleOptionsAsync();
                return View(model);
            }


            TempData["Success"] = "تم تعديل بيانات المستخدم بنجاح";
            return RedirectToAction(nameof(Index));
        }

        // تعطيل/تفعيل بدل الحذف (Business Rule: يتم إيقاف المستخدم بدل الحذف)
        [HttpPost]
        [ValidateAntiForgeryToken]
        [HasPermission("Disable User")]
        public async Task<IActionResult> ToggleActive(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user is null) return NotFound();

            user.IsActive = !user.IsActive;
            if (user.IsActive) user.IsLocked = false;
            user.UpdatedBy = _currentUser.Username;
            user.UpdatedDate = DateTime.UtcNow;
            await _context.SaveChangesAsync();



            TempData["Success"] = user.IsActive ? "تم تفعيل المستخدم" : "تم إيقاف المستخدم";
            return RedirectToAction(nameof(Index));
        }

        [HasPermission("Reset Password")]
        public async Task<IActionResult> ResetPassword(int id)
        {
            var user = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.UserId == id);
            if (user is null) return NotFound();

            return View(new ResetPasswordViewModel { UserId = user.UserId, Username = user.Username });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [HasPermission("Reset Password")]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == model.UserId);
            if (user is null) return NotFound();

            user.PasswordHash = _passwordHasher.HashPassword(model.NewPassword);
            user.FailedLoginAttempts = 0;
            user.UpdatedBy = _currentUser.Username;
            user.UpdatedDate = DateTime.UtcNow;
            await _context.SaveChangesAsync();



            TempData["Success"] = "تم إعادة تعيين كلمة المرور بنجاح";

            return RedirectToAction(nameof(Index));
        }

        private async Task<List<RoleOption>> GetRoleOptionsAsync()
        {
            return await _context.Roles.AsNoTracking()
                .Where(r => r.IsActive)
                .Select(r => new RoleOption { RoleId = r.RoleId, RoleName = r.RoleName })
                .ToListAsync();
        }
    }
}