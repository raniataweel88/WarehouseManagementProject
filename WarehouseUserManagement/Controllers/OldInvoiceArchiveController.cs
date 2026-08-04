using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WarehouseUserManagement.Authorization;
using WarehouseUserManagement.Data;
using WarehouseUserManagement.Models;
using WarehouseUserManagement.Models.ViewModels;
using WarehouseUserManagement.Services;

namespace WarehouseUserManagement.Controllers
{
    /// <summary>
    /// أرشيف الفواتير القديمة: موديول مستقل تماماً عن فواتير الشراء (PurchaseInvoices).
    /// لا Draft/Posted، لا رقم فاتورة أو مورد مطلوب - فقط رفع ملفات (صور/PDF) وحفظها للرجوع
    /// إليها لاحقاً عند الحاجة، بدون أي التزام بإدخال بياناتها أو ربطها بالمخزون.
    /// </summary>
    public class OldInvoiceArchiveController : Controller
    {
        private const string FolderRelative = "uploads/old-invoices";
        private static readonly string[] AllowedContentTypes =
            { "image/jpeg", "image/png", "image/webp", "application/pdf" };
        private const long MaxFileSizeBytes = 15 * 1024 * 1024; // 15MB لكل ملف
        private const int MaxFilesPerBatch = 50;

        private readonly AppDbContext _context;
        private readonly ICurrentUserService _currentUser;
        private readonly IAuditService _audit;
        private readonly IWebHostEnvironment _env;

        public OldInvoiceArchiveController(AppDbContext context, ICurrentUserService currentUser,
            IAuditService audit, IWebHostEnvironment env)
        {
            _context = context;
            _currentUser = currentUser;
            _audit = audit;
            _env = env;
        }

        // GET: /OldInvoiceArchive?searchTerm=&pageNumber=1
        [HasPermission("View Old Invoices Archive")]
        public async Task<IActionResult> Index(OldInvoiceArchiveSearchRequest request)
        {
            var query = _context.OldInvoiceArchives.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(request.SearchTerm))
            {
                var term = request.SearchTerm.Trim();
                query = query.Where(f =>
                    f.FileName.Contains(term) ||
                    (f.SupplierNameNote != null && f.SupplierNameNote.Contains(term)) ||
                    (f.Notes != null && f.Notes.Contains(term)));
            }

            var totalCount = await query.CountAsync();

            var pageSize = request.PageSize is > 0 and <= 100 ? request.PageSize : 24;
            var pageNumber = request.PageNumber < 1 ? 1 : request.PageNumber;

            var files = await query
                .OrderByDescending(f => f.UploadedDate)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(f => new OldInvoiceArchiveRow
                {
                    OldInvoiceArchiveId = f.OldInvoiceArchiveId,
                    FileName = f.FileName,
                    StoredPath = f.StoredPath,
                    ContentType = f.ContentType,
                    FileSizeBytes = f.FileSizeBytes,
                    SupplierNameNote = f.SupplierNameNote,
                    InvoiceDateNote = f.InvoiceDateNote,
                    Notes = f.Notes,
                    UploadedBy = f.UploadedBy,
                    UploadedDate = f.UploadedDate
                })
                .ToListAsync();

            var vm = new OldInvoiceArchiveListViewModel
            {
                Files = files,
                SearchTerm = request.SearchTerm,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalCount = totalCount
            };

            return View(vm);
        }

        // GET: /OldInvoiceArchive/Upload
        [HasPermission("Upload Old Invoices Archive")]
        public IActionResult Upload() => View(new OldInvoiceArchiveUploadViewModel());

        // POST: /OldInvoiceArchive/Upload  (يرفع عدة ملفات دفعة واحدة، كل ملف يصبح سجل أرشيف مستقل)
        [HttpPost]
        [ValidateAntiForgeryToken]
        [HasPermission("Upload Old Invoices Archive")]
        public async Task<IActionResult> Upload(OldInvoiceArchiveUploadViewModel model)
        {
            var files = (model.Files ?? new List<IFormFile>()).Where(f => f is { Length: > 0 }).ToList();

            if (!files.Any())
                ModelState.AddModelError(nameof(model.Files), "اختر ملف واحد على الأقل");
            else if (files.Count > MaxFilesPerBatch)
                ModelState.AddModelError(nameof(model.Files), $"لا يمكن رفع أكثر من {MaxFilesPerBatch} ملف بالمرة الواحدة");
            else
            {
                foreach (var file in files)
                {
                    if (!TryValidateFile(file, out var error))
                    {
                        ModelState.AddModelError(nameof(model.Files), $"{file.FileName}: {error}");
                    }
                }
            }

            if (!ModelState.IsValid)
                return View(model);

            var folderOnDisk = Path.Combine(_env.WebRootPath, FolderRelative);
            Directory.CreateDirectory(folderOnDisk);

            var records = new List<OldInvoiceArchive>();
            foreach (var file in files)
            {
                var extension = Path.GetExtension(file.FileName);
                var storedName = $"{Guid.NewGuid():N}{extension}";
                var fullPath = Path.Combine(folderOnDisk, storedName);

                await using (var stream = new FileStream(fullPath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                records.Add(new OldInvoiceArchive
                {
                    StoredPath = $"{FolderRelative}/{storedName}",
                    FileName = file.FileName,
                    ContentType = file.ContentType,
                    FileSizeBytes = file.Length,
                    SupplierNameNote = string.IsNullOrWhiteSpace(model.SupplierNameNote) ? null : model.SupplierNameNote.Trim(),
                    InvoiceDateNote = model.InvoiceDateNote,
                    Notes = string.IsNullOrWhiteSpace(model.Notes) ? null : model.Notes.Trim(),
                    UploadedBy = _currentUser.Username,
                    UploadedDate = DateTime.UtcNow
                });
            }

            _context.OldInvoiceArchives.AddRange(records);
            await _context.SaveChangesAsync();

            await _audit.LogAsync(_currentUser.UserId, "Upload", "OldInvoiceArchive",
                string.Join(",", records.Select(r => r.OldInvoiceArchiveId)), null,
                $"تم أرشفة {records.Count} ملف");

            TempData["Success"] = $"تم أرشفة {records.Count} ملف بنجاح";
            return RedirectToAction(nameof(Index));
        }

        // GET: /OldInvoiceArchive/Download/5
        [HasPermission("View Old Invoices Archive")]
        public async Task<IActionResult> Download(int id)
        {
            var record = await _context.OldInvoiceArchives.AsNoTracking()
                .FirstOrDefaultAsync(f => f.OldInvoiceArchiveId == id);
            if (record is null) return NotFound();

            var fullPath = Path.Combine(_env.WebRootPath, record.StoredPath.Replace('/', Path.DirectorySeparatorChar));
            if (!System.IO.File.Exists(fullPath))
            {
                TempData["Error"] = "الملف غير موجود على القرص";
                return RedirectToAction(nameof(Index));
            }

            var bytes = await System.IO.File.ReadAllBytesAsync(fullPath);
            return File(bytes, record.ContentType ?? "application/octet-stream", record.FileName);
        }

        // POST: /OldInvoiceArchive/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [HasPermission("Delete Old Invoices Archive")]
        public async Task<IActionResult> Delete(int id)
        {
            var record = await _context.OldInvoiceArchives.FirstOrDefaultAsync(f => f.OldInvoiceArchiveId == id);
            if (record is null) return NotFound();

            var fullPath = Path.Combine(_env.WebRootPath, record.StoredPath.Replace('/', Path.DirectorySeparatorChar));
            if (System.IO.File.Exists(fullPath))
            {
                try { System.IO.File.Delete(fullPath); }
                catch (IOException) { /* لا نمنع حذف السجل إذا تعذّر حذف الملف من القرص */ }
            }

            _context.OldInvoiceArchives.Remove(record);
            await _context.SaveChangesAsync();

            await _audit.LogAsync(_currentUser.UserId, "Delete", "OldInvoiceArchive",
                record.OldInvoiceArchiveId.ToString(), record.FileName, null);

            TempData["Success"] = "تم حذف الملف من الأرشيف";
            return RedirectToAction(nameof(Index));
        }

        private static bool TryValidateFile(IFormFile file, out string? error)
        {
            error = null;

            if (file.Length > MaxFileSizeBytes)
            {
                error = "حجم الملف أكبر من الحد المسموح (15MB)";
                return false;
            }

            if (!AllowedContentTypes.Contains(file.ContentType, StringComparer.OrdinalIgnoreCase))
            {
                error = "نوع الملف غير مدعوم. المسموح: JPG, PNG, WEBP, PDF";
                return false;
            }

            return true;
        }
    }
}
