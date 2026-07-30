using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using WarehouseUserManagement.Data;

namespace WarehouseUserManagement.Services
{
    public interface IPermissionService
    {
        /// <summary>يرجع أسماء صلاحيات الدور. النتيجة مخزنة مؤقتاً (Cached) بحيث تكون الاستدعاءات
        /// المتكررة O(1) بدل الرجوع لقاعدة البيانات في كل مرة.</summary>
        Task<HashSet<string>> GetPermissionNamesForRoleAsync(int roleId);

        /// <summary>يجب استدعاؤها عند تعديل صلاحيات دور معيّن لإفراغ الكاش الخاص به.</summary>
        void InvalidateRoleCache(int roleId);
    }

    public class PermissionService : IPermissionService
    {
        private readonly AppDbContext _context;
        private readonly IMemoryCache _cache;
        private readonly TimeSpan _cacheDuration;

        private static string CacheKey(int roleId) => $"role-permissions:{roleId}";

        public PermissionService(AppDbContext context, IMemoryCache cache, IConfiguration config)
        {
            _context = context;
            _cache = cache;
            var minutes = config.GetValue<int?>("AppSettings:PermissionCacheMinutes") ?? 30;
            _cacheDuration = TimeSpan.FromMinutes(minutes);
        }

        public async Task<HashSet<string>> GetPermissionNamesForRoleAsync(int roleId)
        {
            if (_cache.TryGetValue(CacheKey(roleId), out HashSet<string>? cached) && cached is not null)
                return cached;

            // AsNoTracking: قراءة فقط بدون تتبع تغييرات EF -> أخف وزناً وأسرع
            var names = await _context.RolePermissions
                .AsNoTracking()
                .Where(rp => rp.RoleId == roleId)
                .Select(rp => rp.Permission!.PermissionName)
                .ToListAsync();

            var set = new HashSet<string>(names, StringComparer.OrdinalIgnoreCase);
            _cache.Set(CacheKey(roleId), set, _cacheDuration);
            return set;
        }

        public void InvalidateRoleCache(int roleId) => _cache.Remove(CacheKey(roleId));
    }
}
