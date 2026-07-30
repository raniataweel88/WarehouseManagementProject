using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WarehouseUserManagement.Authorization;
using WarehouseUserManagement.Data;
using WarehouseUserManagement.Models;
using WarehouseUserManagement.Models.ViewModels;
using WarehouseUserManagement.Services;

namespace WarehouseUserManagement.Controllers
{
    public class CategoriesController : Controller
    {
        private readonly AppDbContext _context;
        private readonly ICurrentUserService _currentUser;

        public CategoriesController(
            AppDbContext context,
            ICurrentUserService currentUser)
        {
            _context = context;
            _currentUser = currentUser;
        }


        // GET: Categories
        [HasPermission("View Category")]
        public async Task<IActionResult> Index(CategorySearchRequest request)
        {
            var query = _context.Categories
                .AsNoTracking()
                .Where(c => !c.IsDeleted)
                .AsQueryable();


            // Search
            if (!string.IsNullOrWhiteSpace(request.SearchTerm))
            {
                var term = request.SearchTerm.Trim();

                query = query.Where(c =>
                    c.CategoryName.StartsWith(term));
            }


            // Status Filter
            if (request.StatusFilter.HasValue)
            {
                query = query.Where(c =>
                    c.IsActive == request.StatusFilter.Value);
            }


            var totalCount = await query.CountAsync();


            var pageSize =
                request.PageSize is > 0 and <= 100
                ? request.PageSize
                : 10;


            var pageNumber =
                request.PageNumber < 1
                ? 1
                : request.PageNumber;



            var categories = await query
                .OrderBy(c => c.CategoryName)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)

                .Select(c => new CategoryRow
                {
                    CategoryId = c.CategoryId,
                    CategoryName = c.CategoryName,
                    IsActive = c.IsActive
                })

                .ToListAsync();



            var vm = new CategoryListViewModel
            {
                Categories = categories,

                SearchTerm = request.SearchTerm,

                StatusFilter = request.StatusFilter,

                PageNumber = pageNumber,

                PageSize = pageSize,

                TotalCount = totalCount
            };


            return View(vm);
        }





        // CREATE
        [HttpPost]
        [ValidateAntiForgeryToken]
        [HasPermission("Add Category")]
        public async Task<IActionResult> Create(
            CategoryFormViewModel model)
        {

            if (await _context.Categories
                .AnyAsync(c =>
                    c.CategoryName == model.CategoryName
                    && !c.IsDeleted))
            {
                ModelState.AddModelError(
                    nameof(model.CategoryName),
                    "Category already exists");
            }



            if (!ModelState.IsValid)
            {
                TempData["Error"] =
                    "يرجى التأكد من البيانات المدخلة";

                return RedirectToAction(nameof(Index));
            }


            // الفهرس الفريد على CategoryName يشمل الصفوف المحذوفة ناعماً أيضاً (Soft Delete)،
            // فلو كان هناك تصنيف محذوف سابقاً يحمل نفس الاسم، نعيد إحياء نفس الصف بدل محاولة
            // إدخال صف جديد (وإلا يرمي DbUpdateException / SqlException بسبب تكرار المفتاح
            // رغم أن التصنيف القديم محذوف وغير ظاهر للمستخدم). Query Filter الافتراضي يستثني
            // الصفوف المحذوفة، لذلك نستخدم IgnoreQueryFilters هنا صراحة.
            var deletedMatch = await _context.Categories
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(c =>
                    c.IsDeleted &&
                    c.CategoryName == model.CategoryName);

            if (deletedMatch != null)
            {
                deletedMatch.CategoryName = model.CategoryName;

                deletedMatch.IsActive = model.IsActive;

                deletedMatch.IsDeleted = false;

                deletedMatch.DeletedBy = null;

                deletedMatch.DeletedDate = null;

                deletedMatch.UpdatedBy = _currentUser.Username;

                deletedMatch.UpdatedDate = DateTime.UtcNow;
            }
            else
            {
                var category = new Category
                {
                    CategoryName = model.CategoryName,

                    IsActive = model.IsActive,

                    IsDeleted = false,

                    CreatedBy = _currentUser.Username,

                    CreatedDate = DateTime.UtcNow
                };



                _context.Categories.Add(category);
            }

            await _context.SaveChangesAsync();



            TempData["Success"] =
                "تم إنشاء التصنيف بنجاح";


            return RedirectToAction(nameof(Index));
        }





        // EDIT GET
        [HasPermission("Edit Category")]
        public async Task<IActionResult> Edit(int id)
        {

            var category =
                await _context.Categories
                .AsNoTracking()
                .FirstOrDefaultAsync(c =>
                    c.CategoryId == id);


            if (category == null)
                return NotFound();



            var vm = new CategoryFormViewModel
            {
                CategoryId = category.CategoryId,

                CategoryName = category.CategoryName,

                IsActive = category.IsActive,

                RowVersion = category.RowVersion
            };


            return View(vm);
        }





        // EDIT POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        [HasPermission("Edit Category")]
        public async Task<IActionResult> Edit(
            int id,
            CategoryFormViewModel model)
        {

            if (id != model.CategoryId)
                return NotFound();



            if (await _context.Categories
                .AnyAsync(c =>
                    c.CategoryName == model.CategoryName
                    &&
                    c.CategoryId != id
                    &&
                    !c.IsDeleted))
            {

                ModelState.AddModelError(
                    nameof(model.CategoryName),
                    "Category already exists");
            }




            if (!ModelState.IsValid)
                return View(model);




            var category =
                await _context.Categories
                .FirstOrDefaultAsync(c =>
                    c.CategoryId == id);



            if (category == null)
                return NotFound();


            // إن كان هناك تصنيف محذوف ناعماً (Soft Delete) يحمل نفس الاسم الجديد، نحذفه نهائياً
            // من قاعدة البيانات لتحرير الاسم، لأنه أصلاً غير ظاهر لأي مستخدم ولا قيمة من بقائه.
            var deletedGhost = await _context.Categories
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(c =>
                    c.IsDeleted &&
                    c.CategoryName == model.CategoryName &&
                    c.CategoryId != id);

            if (deletedGhost != null)
            {
                _context.Categories.Remove(deletedGhost);
            }


            if (model.RowVersion != null)
            {
                _context.Entry(category)
                .Property(c => c.RowVersion)
                .OriginalValue = model.RowVersion;
            }





            category.CategoryName =
                model.CategoryName;


            category.IsActive =
                model.IsActive;


            category.UpdatedBy =
                _currentUser.Username;


            category.UpdatedDate =
                DateTime.UtcNow;




            try
            {
                await _context.SaveChangesAsync();
            }

            catch (DbUpdateConcurrencyException)
            {

                ModelState.AddModelError(
                    "",
                    "تم تعديل البيانات من مستخدم آخر");

                return View(model);
            }




            TempData["Success"] =
                "تم تعديل التصنيف بنجاح";


            return RedirectToAction(nameof(Index));
        }





        // SOFT DELETE
        [HttpPost]
        [ValidateAntiForgeryToken]
        [HasPermission("Delete Category")]
        public async Task<IActionResult> Delete(int id)
        {

            var category =
                await _context.Categories
                .FirstOrDefaultAsync(c =>
                    c.CategoryId == id);



            if (category == null)
                return NotFound();




            category.IsDeleted = true;


            category.DeletedBy =
                _currentUser.Username;


            category.DeletedDate =
                DateTime.UtcNow;



            await _context.SaveChangesAsync();



            TempData["Success"] =
                "تم حذف التصنيف بنجاح";


            return RedirectToAction(nameof(Index));
        }





        // RESTORE (اختياري)
        [HttpPost]
        [ValidateAntiForgeryToken]
        [HasPermission("Restore Category")]
        public async Task<IActionResult> Restore(int id)
        {

            var category =
                await _context.Categories
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(c =>
                    c.CategoryId == id);



            if (category == null)
                return NotFound();



            category.IsDeleted = false;


            category.UpdatedBy =
                _currentUser.Username;


            category.UpdatedDate =
                DateTime.UtcNow;



            await _context.SaveChangesAsync();



            TempData["Success"] =
                "تم استرجاع التصنيف بنجاح";


            return RedirectToAction(nameof(Index));
        }




        // ACTIVE / INACTIVE
        [HttpPost]
        [ValidateAntiForgeryToken]
        [HasPermission("Deactivate Category")]
        public async Task<IActionResult> ToggleActive(int id)
        {

            var category =
                await _context.Categories
                .FirstOrDefaultAsync(c =>
                    c.CategoryId == id);



            if (category == null)
                return NotFound();



            category.IsActive =
                !category.IsActive;



            category.UpdatedBy =
                _currentUser.Username;


            category.UpdatedDate =
                DateTime.UtcNow;



            await _context.SaveChangesAsync();



            TempData["Success"] =
                category.IsActive
                ? "تم تفعيل التصنيف"
                : "تم إيقاف التصنيف";



            return RedirectToAction(nameof(Index));
        }
    }
}