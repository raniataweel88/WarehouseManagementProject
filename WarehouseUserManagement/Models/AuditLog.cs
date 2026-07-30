namespace WarehouseUserManagement.Models
{
    /// <summary>سجل العمليات - tracks every sensitive action (Business Rule #11).</summary>
    public class AuditLog
    {
        public long AuditId { get; set; }

        public int? UserId { get; set; }
        public User? User { get; set; }

        public string Action { get; set; } = string.Empty;      // e.g. "Update Product"
        public string? TableName { get; set; }
        public string? RecordId { get; set; }
        public string? OldValue { get; set; }
        public string? NewValue { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    }
}
