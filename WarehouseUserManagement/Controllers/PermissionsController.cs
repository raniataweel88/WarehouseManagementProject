using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WarehouseUserManagement.Authorization;
using WarehouseUserManagement.Data;
using WarehouseUserManagement.Models;
using WarehouseUserManagement.Services;

namespace WarehouseUserManagement.Controllers
{
    [HasPermission("Manage Permissions")]
    public class PermissionsController : Controller
    {
        private readonly AppDbContext _context;
         private readonly ICurrentUserService _currentUser;

        public PermissionsController(AppDbContext context , ICurrentUserService currentUser)
        {
            _context = context;
             
            _currentUser = currentUser;
        }

        public async Task<IActionResult> Index()
        {
            var permissions = await _context.Permissions.AsNoTracking()
                .OrderBy(p => p.ModuleName).ThenBy(p => p.PermissionName)
                .ToListAsync();
            return View(permissions);
        }

        public IActionResult Create() => View(new Permission());

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Permission model)
        {
            if (await _context.Permissions.AnyAsync(p => p.PermissionName == model.PermissionName))
                ModelState.AddModelError(nameof(model.PermissionName), "اسم الصلاحية موجود مسبقاً");

            if (!ModelState.IsValid) return View(model);

            _context.Permissions.Add(model);
            await _context.SaveChangesAsync();

          

            TempData["Success"] = "تم إضافة الصلاحية بنجاح";
            return RedirectToAction(nameof(Index));
        }
    }
}
