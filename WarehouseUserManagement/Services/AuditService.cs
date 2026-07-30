using WarehouseUserManagement.Data;
using WarehouseUserManagement.Models;

namespace WarehouseUserManagement.Services
{
    public interface IAuditService
    {
        Task LogAsync(int? userId, string action, string? tableName = null,
            string? recordId = null, string? oldValue = null, string? newValue = null);
    }

    public class AuditService : IAuditService
    {
        private readonly AppDbContext _context;

        public AuditService(AppDbContext context) => _context = context;

        public async Task LogAsync(int? userId, string action, string? tableName = null,
            string? recordId = null, string? oldValue = null, string? newValue = null)
        {
            _context.AuditLogs.Add(new AuditLog
            {
                UserId = userId,
                Action = action,
                TableName = tableName,
                RecordId = recordId,
                OldValue = oldValue,
                NewValue = newValue,
                CreatedDate = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();
        }
    }
}
