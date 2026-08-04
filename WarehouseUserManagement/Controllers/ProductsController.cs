using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WarehouseUserManagement.Authorization;
using WarehouseUserManagement.Data;
using WarehouseUserManagement.Models;
using WarehouseUserManagement.Models.ViewModels;
using WarehouseUserManagement.Services;

namespace WarehouseUserManagement.Controllers
{
    public class ProductsController : Controller
    {
        private readonly AppDbContext _context;
        private readonly ICurrentUserService _currentUser;


        public ProductsController(
            AppDbContext context,
            ICurrentUserService currentUser)
        {
            _context = context;
            _currentUser = currentUser;
        }



        // ============================
        // Index
        // ============================

        [HasPermission("View Product")]
        public async Task<IActionResult> Index(ProductSearchRequest request)
        {

            var query = _context.Products
                .Where(p => !p.IsDeleted)
                .Include(p => p.Category)
                .AsNoTracking()
                .AsQueryable();



            if (!string.IsNullOrWhiteSpace(request.SearchTerm))
            {
                var term = request.SearchTerm.Trim();

                query = query.Where(p =>
                    p.ProductName.Contains(term) ||
                    (p.Barcode != null &&
                     p.Barcode.Contains(term)));
            }



            if (request.CategoryFilter.HasValue)
            {
                query = query.Where(p =>
                    p.CategoryId ==
                    request.CategoryFilter.Value);
            }



            if (request.StatusFilter.HasValue)
            {
                query = query.Where(p =>
                    p.IsActive ==
                    request.StatusFilter.Value);
            }



            if (request.LowStockOnly)
            {
                query = query.Where(p =>
                    p.CurrentQuantity <=
                    p.MinimumQuantity);
            }



            var totalCount =
                await query.CountAsync();



            int pageSize =
                request.PageSize > 0
                ? request.PageSize
                : 10;



            int pageNumber =
                request.PageNumber > 0
                ? request.PageNumber
                : 1;



            var products =
                await query
                .OrderBy(p => p.ProductName)

                .Skip((pageNumber - 1) * pageSize)

                .Take(pageSize)

                .Select(p => new ProductRow
                {

                    ProductId = p.ProductId,

                    ProductName = p.ProductName,

                    CategoryName =
                    p.Category!.CategoryName,

                    Barcode = p.Barcode,

                    Unit = p.Unit,


                    CurrentQuantity =
                    p.CurrentQuantity,


                    AverageCost =
                    p.AverageCost,


                    MinimumQuantity =
                    p.MinimumQuantity,


                    IsActive =
                    p.IsActive,



                })
                .ToListAsync();




            var vm = new ProductListViewModel
            {

                Products = products,

                SearchTerm = request.SearchTerm,

                CategoryFilter = request.CategoryFilter,

                StatusFilter = request.StatusFilter,

                LowStockOnly = request.LowStockOnly,

                PageNumber = pageNumber,

                PageSize = pageSize,

                TotalCount = totalCount,


                CategoryOptions =
                await GetCategories()

            };


            return View(vm);
        }







        // ============================
        // Create
        // ============================


        [HttpPost]
        [ValidateAntiForgeryToken]
        [HasPermission("Add Product")]
        public async Task<IActionResult> Create(
            ProductFormViewModel model)
        {



            try
            {
                await ValidateProduct(model);



                if (!ModelState.IsValid)
                {
                    TempData["Error"] =
                        "يرجى التأكد من البيانات";

                    return RedirectToAction(nameof(Index));
                }





                var barcode =
                    string.IsNullOrWhiteSpace(model.Barcode)
                    ? null
                    : model.Barcode.Trim();

                // الفهرس الفريد على Barcode / (CategoryId + ProductName) يشمل الصفوف المحذوفة ناعماً
                // أيضاً على مستوى قاعدة البيانات، فلو صنف محذوف كان يحمل نفس الباركود أو نفس الاسم،
                // نعيد إحياء نفس الصف بدل محاولة إدخال صف جديد (وإلا سيرمي DbUpdateException
                // ويظهر خطأ "الباركود مستخدم مسبقاً" رغم أن الصنف القديم محذوف وغير ظاهر للمستخدم).
                var deletedMatch = await _context.Products.FirstOrDefaultAsync(p =>
                    p.IsDeleted &&
                    (
                        (barcode != null && p.Barcode == barcode) ||
                        (p.CategoryId == model.CategoryId && p.ProductName == model.ProductName.Trim())
                    ));

                if (deletedMatch != null)
                {
                    deletedMatch.ProductName = model.ProductName.Trim();
                    deletedMatch.CategoryId = model.CategoryId;
                    deletedMatch.Barcode = barcode;
                    deletedMatch.Unit = model.Unit;
                    deletedMatch.CurrentQuantity = model.CurrentQuantity;
                    deletedMatch.AverageCost = model.AverageCost;
                    deletedMatch.MinimumQuantity = model.MinimumQuantity;
                    deletedMatch.IsActive = model.IsActive;
                    deletedMatch.IsDeleted = false;
                    deletedMatch.UpdatedBy = _currentUser.Username;
                    deletedMatch.UpdatedDate = DateTime.UtcNow;
                }
                else
                {
                    var product = new Product
                    {
                        ProductName = model.ProductName.Trim(),
                        CategoryId = model.CategoryId,
                        Barcode = barcode,
                        Unit = model.Unit,
                        CurrentQuantity = model.CurrentQuantity,
                        AverageCost = model.AverageCost,
                        MinimumQuantity = model.MinimumQuantity,
                        IsActive = model.IsActive,
                        IsDeleted = false,
                        CreatedBy = _currentUser.Username,
                        CreatedDate = DateTime.UtcNow
                    };

                    _context.Products.Add(product);
                }

                await _context.SaveChangesAsync();



                TempData["Success"] =
                    "تم إضافة الصنف بنجاح";



            }
            catch (DbUpdateException ex)
            {
                if (ex.InnerException?.Message.Contains("IX_Products_Barcode") == true)
                {
                    TempData["Error"] = "رقم الباركود مستخدم مسبقاً، يرجى إدخال باركود آخر.";
                }
                else
                {
                    TempData["Error"] = "حدث خطأ أثناء حفظ البيانات، يرجى المحاولة مرة أخرى.";
                }
            }

            return RedirectToAction(nameof(Index));

        }









        // ============================
        // Quick Create (AJAX)
        // تُستخدم من داخل شاشة فاتورة الشراء لإنشاء صنف جديد دون مغادرة الفاتورة.
        // نفس قواعد التحقق المستخدمة في الإنشاء العادي (ValidateProduct)، وتُعيد الصنف الجديد كـ JSON
        // ليُضاف تلقائياً لقائمة الأصناف داخل الفاتورة ويُختار في السطر الحالي.
        // ============================

        [HttpPost]
        [ValidateAntiForgeryToken]
        [HasPermission("Add Product")]
        public async Task<IActionResult> QuickCreate(ProductFormViewModel model)
        {
            await ValidateProduct(model);

            if (!ModelState.IsValid)
            {
                var errors = ModelState
                    .Where(kv => kv.Value != null && kv.Value.Errors.Count > 0)
                    .SelectMany(kv => kv.Value!.Errors.Select(e => e.ErrorMessage))
                    .ToList();

                return BadRequest(new { success = false, errors });
            }

            try
            {
                var barcode = string.IsNullOrWhiteSpace(model.Barcode) ? null : model.Barcode.Trim();

                // نفس منطق Create: إعادة إحياء صنف محذوف سابقاً بنفس الباركود/الاسم بدل الاصطدام
                // بالفهرس الفريد على قاعدة البيانات (يشمل الصفوف المحذوفة ناعماً أيضاً).
                var deletedMatch = await _context.Products.FirstOrDefaultAsync(p =>
                    p.IsDeleted &&
                    (
                        (barcode != null && p.Barcode == barcode) ||
                        (p.CategoryId == model.CategoryId && p.ProductName == model.ProductName.Trim())
                    ));

                Product product;

                if (deletedMatch != null)
                {
                    product = deletedMatch;
                    product.ProductName = model.ProductName.Trim();
                    product.CategoryId = model.CategoryId;
                    product.Barcode = barcode;
                    product.Unit = model.Unit;
                    product.CurrentQuantity = 0;
                    product.AverageCost = 0;
                    product.MinimumQuantity = model.MinimumQuantity;
                    product.IsActive = true;
                    product.IsDeleted = false;
                    product.UpdatedBy = _currentUser.Username;
                    product.UpdatedDate = DateTime.UtcNow;
                }
                else
                {
                    product = new Product
                    {
                        ProductName = model.ProductName.Trim(),
                        CategoryId = model.CategoryId,
                        Barcode = barcode,
                        Unit = model.Unit,
                        CurrentQuantity = 0,
                        AverageCost = 0,
                        MinimumQuantity = model.MinimumQuantity,
                        IsActive = true,
                        IsDeleted = false,
                        CreatedBy = _currentUser.Username,
                        CreatedDate = DateTime.UtcNow
                    };

                    _context.Products.Add(product);
                }

                await _context.SaveChangesAsync();

                var categoryName = await _context.Categories
                    .Where(c => c.CategoryId == product.CategoryId)
                    .Select(c => c.CategoryName)
                    .FirstOrDefaultAsync() ?? string.Empty;

                return Json(new
                {
                    success = true,
                    product = new ProductOption
                    {
                        ProductId = product.ProductId,
                        ProductName = product.ProductName,
                        CategoryName = categoryName,
                        Unit = product.Unit,
                        AverageCost = product.AverageCost,
                        Barcode = product.Barcode
                    }
                });
            }
            catch (DbUpdateException ex)
            {
                var message = ex.InnerException?.Message.Contains("IX_Products_Barcode") == true
                    ? "رقم الباركود مستخدم مسبقاً، يرجى إدخال باركود آخر."
                    : "حدث خطأ أثناء حفظ البيانات، يرجى المحاولة مرة أخرى.";

                return BadRequest(new { success = false, errors = new[] { message } });
            }
        }

        // ============================
        // Edit GET
        // ============================


        [HasPermission("Edit Product")]
        public async Task<IActionResult> Edit(int id)
        {

            var product =
                await _context.Products
                .AsNoTracking()
                .FirstOrDefaultAsync(p =>
                    p.ProductId == id &&
                    !p.IsDeleted);



            if (product == null)
                return NotFound();




            var model = new ProductFormViewModel
            {

                ProductId =
                product.ProductId,


                ProductName =
                product.ProductName,


                CategoryId =
                product.CategoryId,


                Barcode =
                product.Barcode,


                Unit =
                product.Unit,



                CurrentQuantity =
                (int)product.CurrentQuantity,



                AverageCost =
                (int)product.AverageCost,



                MinimumQuantity =
                product.MinimumQuantity,


                IsActive =
                product.IsActive,



                CategoryOptions =
                await GetCategories(product.CategoryId)

            };



            return PartialView("Edit", model);
        }









        // ============================
        // Edit POST
        // ============================


        [HttpPost]
        [ValidateAntiForgeryToken]
        [HasPermission("Edit Product")]
        public async Task<IActionResult> Edit(
            int id,
            ProductFormViewModel model)
        {


            if (id != model.ProductId)
                return NotFound();



            await ValidateProduct(model);



            if (!ModelState.IsValid)
            {

                model.CategoryOptions =
                    await GetCategories(model.CategoryId);


                return PartialView("Edit", model);
            }




            var product =
                await _context.Products
                .FirstOrDefaultAsync(p =>
                    p.ProductId == id &&
                    !p.IsDeleted);




            if (product == null)
                return NotFound();




            product.ProductName =
                model.ProductName.Trim();



            product.CategoryId =
                model.CategoryId;



            product.Barcode =
                string.IsNullOrWhiteSpace(model.Barcode)
                ? null
                : model.Barcode.Trim();



            product.Unit =
                model.Unit;



            // الكمية والسعر لا يتم تعديلهما من شاشة تعديل الصنف
            // (يتم تحديثهما من خلال فواتير الشراء / الصرف / الجرد فقط)
            // لذلك نتجاهل القيم القادمة من النموذج ونُبقي القيم الحالية كما هي.



            product.MinimumQuantity =
                model.MinimumQuantity;



            product.IsActive =
                model.IsActive;




            product.UpdatedBy =
                _currentUser.Username;



            product.UpdatedDate =
                DateTime.UtcNow;




            await _context.SaveChangesAsync();



            TempData["Success"] =
                "تم تعديل الصنف بنجاح";



            return RedirectToAction(nameof(Index));

        }









        // ============================
        // Details
        // ============================


        [HasPermission("View Product")]
        public async Task<IActionResult> Details(int id)
        {

            var product =
                await _context.Products
                .Include(p => p.Category)
                .AsNoTracking()
                .FirstOrDefaultAsync(p =>
                    p.ProductId == id &&
                    !p.IsDeleted);



            if (product == null)
                return NotFound();



            return PartialView("Details", product);

        }










        // ============================
        // Soft Delete
        // ============================

        [HttpPost]
        [ValidateAntiForgeryToken]
        [HasPermission("Disable Product")]
        public async Task<IActionResult> Delete(int id)
        {
            var product = await _context.Products.FindAsync(id);

            if (product == null)
            {
                TempData["Error"] = "الصنف غير موجود";
                return RedirectToAction(nameof(Index));
            }

            // لا يسمح بحذف صنف لديه كمية متبقية في المخزون - يجب تصفير الكمية أولاً
            // (صرف/تسوية) قبل الحذف، حفاظاً على دقة أرصدة المخزون.
            if (product.CurrentQuantity != 0)
            {
                TempData["Error"] = "لا يمكن حذف صنف لديه كمية في المخزون، يجب أن تكون الكمية الحالية صفر أولاً";
                return RedirectToAction(nameof(Index));
            }

            product.IsDeleted = true;

            product.UpdatedBy = _currentUser.Username;
            product.UpdatedDate = DateTime.UtcNow;


            await _context.SaveChangesAsync();


            TempData["Success"] = "تم حذف الصنف بنجاح";


            return RedirectToAction(nameof(Index));
        }






        // ============================
        // Validation
        // ============================


        private async Task ValidateProduct(
            ProductFormViewModel model)
        {

            var category =
                await _context.Categories
                .FirstOrDefaultAsync(c =>
                    c.CategoryId ==
                    model.CategoryId);



            if (category == null)
            {
                ModelState.AddModelError(
                    "CategoryId",
                    "التصنيف غير موجود");
            }



            if (model.CurrentQuantity < 0)
            {
                ModelState.AddModelError(
                    "CurrentQuantity",
                    "الكمية غير صحيحة");
            }



            if (model.AverageCost < 0)
            {
                ModelState.AddModelError(
                    "AverageCost",
                    "السعر غير صحيح");
            }




            var nameExists =
                await _context.Products.AnyAsync(p =>

                    p.ProductName ==
                    model.ProductName &&

                    p.CategoryId ==
                    model.CategoryId &&

                    p.ProductId !=
                    model.ProductId &&

                    !p.IsDeleted
                );



            if (nameExists)
            {
                ModelState.AddModelError(
                    "ProductName",
                    "يوجد صنف بنفس الاسم");
            }

        }








        private async Task<List<CategoryOption>> GetCategories(
            int? selected = null)
        {

            return await _context.Categories

                .Where(c =>
                    c.IsActive ||
                    c.CategoryId == selected)


                .OrderBy(c =>
                    c.CategoryName)


                .Select(c => new CategoryOption
                {

                    CategoryId =
                    c.CategoryId,
                    CategoryName =
                    c.CategoryName

                })


                .ToListAsync();

        }

    }
}