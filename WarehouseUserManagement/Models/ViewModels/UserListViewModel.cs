namespace WarehouseUserManagement.Models.ViewModels
{
    /// <summary>
    /// نتيجة بحث/عرض مستخدمين مع Paging على مستوى SQL Server (OFFSET/FETCH)
    /// بدل تحميل كل الجدول للذاكرة -> يبقي الاستعلام O(log n + PageSize) بفضل الفهرس
    /// على Username بدل O(n) لكل صفحة.
    /// </summary>
    public class UserListViewModel
    {
        public List<UserRow> Users { get; set; } = new();
        public string? SearchTerm { get; set; }
        public string? RoleFilter { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public int TotalCount { get; set; }
        public List<RoleOption> AvailableRoles { get; set; } = new();
        public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    }

    public class UserRow
    {
        public int UserId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string RoleName { get; set; } = string.Empty;
        public string? Email { get; set; }
        public bool IsActive { get; set; }
        public bool IsLocked { get; set; }
        public DateTime? LastLoginDate { get; set; }
    }
}
