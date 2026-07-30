using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WarehouseUserManagement.Authorization;
using WarehouseUserManagement.Data;

namespace WarehouseUserManagement.Controllers
{
    [HasPermission("View Audit Log")]
    public class AuditLogsController : Controller
    {
        private readonly AppDbContext _context;

        public AuditLogsController(AppDbContext context) => _context = context;

        public async Task<IActionResult> Index(int pageNumber = 1, DateTime? from = null, DateTime? to = null)
        {
            var query = _context.AuditLogs.AsNoTracking().Include(a => a.User).AsQueryable();

            // فلترة على عمود CreatedDate المفهرس -> Range Scan فعّال بدل قراءة الجدول كاملاً
            if (from.HasValue) query = query.Where(a => a.CreatedDate >= from.Value);
            if (to.HasValue) query = query.Where(a => a.CreatedDate <= to.Value);

            var pageSize = 20;
            var totalCount = await query.CountAsync();

            var logs = await query
                .OrderByDescending(a => a.CreatedDate) // مرتب على عمود مفهرس
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.PageNumber = pageNumber;
            ViewBag.TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
            ViewBag.From = from;
            ViewBag.To = to;

            return View(logs);
        }
    }
}
