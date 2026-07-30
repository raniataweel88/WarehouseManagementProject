using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WarehouseUserManagement.Authorization;
using WarehouseUserManagement.Data;
using WarehouseUserManagement.Models;
using WarehouseUserManagement.Models.ViewModels;
using WarehouseUserManagement.Services;

namespace WarehouseUserManagement.Controllers
{
    public class DepartmentsController : Controller
    {
        private readonly AppDbContext _context;
        private readonly ICurrentUserService _currentUser;

        public DepartmentsController(AppDbContext context, ICurrentUserService currentUser)
        {
            _context = context;
            _currentUser = currentUser;
        }

        // GET: /Departments?searchTerm=&statusFilter=&pageNumber=1
        [HasPermission("View Departments")]
        public async Task<IActionResult> Index(DepartmentSearchRequest request)
        {
            var query = _context.Departments.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(request.SearchTerm))
            {
                var term = request.SearchTerm.Trim();
                // StartsWith يتيح استخدام الفهرس على DepartmentName (Index Seek O(log n))
                // بعكس Contains الذي يجبر على Full Scan O(n).
                query = query.Where(d => d.DepartmentName.StartsWith(term));
            }

            if (request.StatusFilter.HasValue)
                query = query.Where(d => d.IsActive == request.StatusFilter.Value);

            var totalCount = await query.CountAsync();

            var pageSize = request.PageSize is > 0 and <= 100 ? request.PageSize : 10;
            var pageNumber = request.PageNumber < 1 ? 1 : request.PageNumber;

            // Paging على مستوى قاعدة البيانات (OFFSET/FETCH) بدل تحميل كل الجدول للذاكرة
            var departments = await query
                .OrderBy(d => d.DepartmentName) // ترتيب حسب عمود مفهرس
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(d => new DepartmentRow
                {
                    DepartmentId = d.DepartmentId,
                    DepartmentName = d.DepartmentName,
                    Description = d.Description,
                    IsActive = d.IsActive
                })
                .ToListAsync();

            var vm = new DepartmentListViewModel
            {
                Departments = departments,
                SearchTerm = request.SearchTerm,
                StatusFilter = request.StatusFilter,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalCount = totalCount
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [HasPermission("Add Department")]
        public async Task<IActionResult> Create(DepartmentFormViewModel model)
        {
            // Unique check يعتمد على الفهرس الفريد على DepartmentName -> O(log n)
            if (await _context.Departments.AnyAsync(d => d.DepartmentName == model.DepartmentName))
                ModelState.AddModelError(nameof(model.DepartmentName), "Department already exists");

            if (!ModelState.IsValid)
            {
                TempData["Error"] = "يرجى التأكد من البيانات المدخلة";
                return RedirectToAction(nameof(Index));
            }

            var department = new Department
            {
                DepartmentName = model.DepartmentName,
                Description = model.Description,
                IsActive = model.IsActive,
                CreatedBy = _currentUser.Username,
                CreatedDate = DateTime.UtcNow
            };

            _context.Departments.Add(department);
            await _context.SaveChangesAsync();

            TempData["Success"] = "تم إنشاء القسم بنجاح";
            return RedirectToAction(nameof(Index));
        }

        [HasPermission("Edit Department")]
        public async Task<IActionResult> Edit(int id)
        {
            var department = await _context.Departments.AsNoTracking()
                .FirstOrDefaultAsync(d => d.DepartmentId == id);
            if (department is null) return NotFound();

            var vm = new DepartmentFormViewModel
            {
                DepartmentId = department.DepartmentId,
                DepartmentName = department.DepartmentName,
                Description = department.Description,
                IsActive = department.IsActive,
                RowVersion = department.RowVersion
            };
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [HasPermission("Edit Department")]
        public async Task<IActionResult> Edit(int id, DepartmentFormViewModel model)
        {
            if (id != model.DepartmentId) return NotFound();

            if (await _context.Departments.AnyAsync(d => d.DepartmentName == model.DepartmentName && d.DepartmentId != id))
                ModelState.AddModelError(nameof(model.DepartmentName), "Department already exists");

            if (!ModelState.IsValid)
                return View(model);

            var department = await _context.Departments.FirstOrDefaultAsync(d => d.DepartmentId == id);
            if (department is null) return NotFound();

            if (model.RowVersion is not null)
                _context.Entry(department).Property(d => d.RowVersion).OriginalValue = model.RowVersion;

            department.DepartmentName = model.DepartmentName;
            department.Description = model.Description;
            department.IsActive = model.IsActive;
            department.UpdatedBy = _currentUser.Username;
            department.UpdatedDate = DateTime.UtcNow;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                ModelState.AddModelError(string.Empty, "تم تعديل هذا القسم من جلسة أخرى، الرجاء إعادة تحميل الصفحة");
                return View(model);
            }

            TempData["Success"] = "تم تعديل بيانات القسم بنجاح";
            return RedirectToAction(nameof(Index));
        }

        // حذف ناعم نهائي بدون واجهة استرجاع (Business Rule: الأقسام القديمة تبقى للحفاظ على تاريخ العمليات)
        // ملاحظة: هذا الـ Action كان مفقوداً سابقاً بينما الـ View يستدعي asp-action="Delete" -> كان يسبب 404 عند الضغط على "حذف"
        [HttpPost]
        [ValidateAntiForgeryToken]
        [HasPermission("Deactivate Department")]
        public async Task<IActionResult> Delete(int id)
        {
            var department = await _context.Departments.FindAsync(id);
            if (department is null) return NotFound();

            department.IsActive = false;
            department.UpdatedBy = _currentUser.Username;
            department.UpdatedDate = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            TempData["Success"] = "تم حذف القسم";
            return RedirectToAction(nameof(Index));
        }
    }
}