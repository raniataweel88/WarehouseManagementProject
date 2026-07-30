using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WarehouseUserManagement.Authorization;
using WarehouseUserManagement.Data;
using WarehouseUserManagement.Models;
using WarehouseUserManagement.Models.ViewModels;
using WarehouseUserManagement.Services;

namespace WarehouseUserManagement.Controllers
{
    [HasPermission("Manage Roles")]
    public class RolesController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IPermissionService _permissionService;
         private readonly ICurrentUserService _currentUser;

        public RolesController(AppDbContext context, IPermissionService permissionService,
             ICurrentUserService currentUser)
        {
            _context = context;
            _permissionService = permissionService;
             _currentUser = currentUser;
        }

        public async Task<IActionResult> Index()
        {
            // Include + Count باستعلام واحد بدل N+1
            var roles = await _context.Roles.AsNoTracking()
                .Select(r => new RoleListItem
                {
                    RoleId = r.RoleId,
                    RoleName = r.RoleName,
                    Description = r.Description,
                    IsActive = r.IsActive,
                    UsersCount = r.Users.Count(),
                    PermissionsCount = r.RolePermissions.Count()
                })
                .ToListAsync();

            return View(roles);
        }

        public async Task<IActionResult> Create()
        {
            var vm = new RoleFormViewModel { PermissionGroups = await BuildPermissionGroupsAsync(new HashSet<int>()) };
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(RoleFormViewModel model)
        {
            if (await _context.Roles.AnyAsync(r => r.RoleName == model.RoleName))
                ModelState.AddModelError(nameof(model.RoleName), "اسم الدور مستخدم مسبقاً");

            if (!ModelState.IsValid)
            {
                model.PermissionGroups = await BuildPermissionGroupsAsync(new HashSet<int>(model.SelectedPermissionIds));
                return View(model);
            }

            var role = new Role
            {
                RoleName = model.RoleName,
                Description = model.Description,
                IsActive = model.IsActive
            };
            _context.Roles.Add(role);
            await _context.SaveChangesAsync();

            // ربط الصلاحيات المختارة دفعة واحدة (Bulk Insert) بدل استعلام لكل صلاحية
            var selected = new HashSet<int>(model.SelectedPermissionIds); // O(1) Contains أثناء البناء
            _context.RolePermissions.AddRange(
                selected.Select(pid => new RolePermission { RoleId = role.RoleId, PermissionId = pid }));
            await _context.SaveChangesAsync();

      
            TempData["Success"] = "تم إنشاء الدور بنجاح";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            var role = await _context.Roles.AsNoTracking().FirstOrDefaultAsync(r => r.RoleId == id);
            if (role is null) return NotFound();

            var selectedIds = await _context.RolePermissions.AsNoTracking()
                .Where(rp => rp.RoleId == id)
                .Select(rp => rp.PermissionId)
                .ToListAsync();

            var vm = new RoleFormViewModel
            {
                RoleId = role.RoleId,
                RoleName = role.RoleName,
                Description = role.Description,
                IsActive = role.IsActive,
                SelectedPermissionIds = selectedIds,
                PermissionGroups = await BuildPermissionGroupsAsync(new HashSet<int>(selectedIds))
            };
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, RoleFormViewModel model)
        {
            if (id != model.RoleId) return NotFound();

            if (await _context.Roles.AnyAsync(r => r.RoleName == model.RoleName && r.RoleId != id))
                ModelState.AddModelError(nameof(model.RoleName), "اسم الدور مستخدم مسبقاً");

            if (!ModelState.IsValid)
            {
                model.PermissionGroups = await BuildPermissionGroupsAsync(new HashSet<int>(model.SelectedPermissionIds));
                return View(model);
            }

            var role = await _context.Roles.FirstOrDefaultAsync(r => r.RoleId == id);
            if (role is null) return NotFound();

            role.RoleName = model.RoleName;
            role.Description = model.Description;
            role.IsActive = model.IsActive;

            // إعادة بناء الربط بين Role/Permissions: نحذف القديم بدفعة واحدة ونضيف الجديد بدفعة واحدة
            var existing = await _context.RolePermissions.Where(rp => rp.RoleId == id).ToListAsync();
            _context.RolePermissions.RemoveRange(existing);

            var selected = new HashSet<int>(model.SelectedPermissionIds);
            _context.RolePermissions.AddRange(
                selected.Select(pid => new RolePermission { RoleId = id, PermissionId = pid }));

            await _context.SaveChangesAsync();

            // إفراغ الكاش فوراً حتى تنعكس الصلاحيات الجديدة على أول طلب قادم لمستخدمي هذا الدور
            _permissionService.InvalidateRoleCache(id);

       
            TempData["Success"] = "تم تعديل الدور بنجاح";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleActive(int id)
        {
            var role = await _context.Roles.FindAsync(id);
            if (role is null) return NotFound();

            role.IsActive = !role.IsActive;
            await _context.SaveChangesAsync();

       

            TempData["Success"] = role.IsActive ? "تم تفعيل الدور" : "تم إيقاف الدور";
            return RedirectToAction(nameof(Index));
        }

        private async Task<List<PermissionGroup>> BuildPermissionGroupsAsync(HashSet<int> selectedIds)
        {
            var permissions = await _context.Permissions.AsNoTracking()
                .OrderBy(p => p.ModuleName).ThenBy(p => p.PermissionName)
                .ToListAsync();

            return permissions
                .GroupBy(p => p.ModuleName)
                .Select(g => new PermissionGroup
                {
                    ModuleName = g.Key,
                    Permissions = g.Select(p => new PermissionOption
                    {
                        PermissionId = p.PermissionId,
                        PermissionName = p.PermissionName,
                        IsSelected = selectedIds.Contains(p.PermissionId) // O(1)
                    }).ToList()
                })
                .ToList();
        }
    }
}
