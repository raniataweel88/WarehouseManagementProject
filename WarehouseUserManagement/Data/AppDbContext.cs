using Microsoft.EntityFrameworkCore;
using WarehouseUserManagement.Models;

namespace WarehouseUserManagement.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<Permission> Permissions { get; set; }
        public DbSet<RolePermission> RolePermissions { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }
        public DbSet<Department> Departments { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<PurchaseInvoice> PurchaseInvoices { get; set; }
        public DbSet<PurchaseInvoiceDetail> PurchaseInvoiceDetails { get; set; }
        public DbSet<Issue> Issues { get; set; }
        public DbSet<IssueDetail> IssueDetails { get; set; }
        public DbSet<StockTake> StockTakes { get; set; }
        public DbSet<StockTakeDetail> StockTakeDetails { get; set; }
        public DbSet<StockTransaction> StockTransactions { get; set; }
        public DbSet<OldInvoiceArchive> OldInvoiceArchives { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ===== Users =====
            modelBuilder.Entity<User>(e =>
            {
                // فهرس فريد على Username -> يحول البحث عند تسجيل الدخول من O(n) Scan إلى O(log n) Index Seek
                e.HasIndex(u => u.Username).IsUnique();
                // فهرس على RoleId لتسريع الفلترة/الـ Joins حسب الدور
                e.HasIndex(u => u.RoleId);
                // فهرس على IsActive لتسريع تقارير المستخدمين النشطين
                e.HasIndex(u => u.IsActive);

                e.HasOne(u => u.Role)
                 .WithMany(r => r.Users)
                 .HasForeignKey(u => u.RoleId)
                 .OnDelete(DeleteBehavior.Restrict); // لا يسمح بحذف Role له مستخدمون
            });

            // ===== Roles =====
            modelBuilder.Entity<Role>(e =>
            {
                e.HasIndex(r => r.RoleName).IsUnique();
            });

            // ===== Permissions =====
            modelBuilder.Entity<Permission>(e =>
            {
                e.HasIndex(p => p.PermissionName).IsUnique();
                e.HasIndex(p => p.ModuleName);
            });

            // ===== RolePermissions (Many-To-Many bridge) =====
            modelBuilder.Entity<RolePermission>(e =>
            {
                // فهرس فريد مركب يمنع تكرار نفس الصلاحية لنفس الدور، ويسرّع تحميل صلاحيات الدور O(log n)
                e.HasIndex(rp => new { rp.RoleId, rp.PermissionId }).IsUnique();

                e.HasOne(rp => rp.Role)
                 .WithMany(r => r.RolePermissions)
                 .HasForeignKey(rp => rp.RoleId)
                 .OnDelete(DeleteBehavior.Cascade);

                e.HasOne(rp => rp.Permission)
                 .WithMany(p => p.RolePermissions)
                 .HasForeignKey(rp => rp.PermissionId)
                 .OnDelete(DeleteBehavior.Cascade);
            });

            // ===== AuditLogs =====
            modelBuilder.Entity<AuditLog>(e =>
            {
                // AuditId لا يطابق اتفاقية EF التلقائية للمفتاح الأساسي ({TypeName}Id = AuditLogId)
                // لذلك يجب تحديده صراحة هنا وإلا يرمي EF استثناء "requires a primary key".
                e.HasKey(a => a.AuditId);

                e.HasIndex(a => a.UserId);
                // فهرس على التاريخ لتسريع فرز/فلترة التقارير الزمنية O(log n) بدل فرز الجدول كاملاً
                e.HasIndex(a => a.CreatedDate);

                e.HasOne(a => a.User)
                 .WithMany()
                 .HasForeignKey(a => a.UserId)
                 .OnDelete(DeleteBehavior.SetNull);
            });

            // ===== Departments =====
            modelBuilder.Entity<Department>(e =>
            {
                // فهرس فريد على DepartmentName -> يمنع التكرار ويحول البحث إلى O(log n) Index Seek
                e.HasIndex(d => d.DepartmentName).IsUnique();
                // فهرس على IsActive لتسريع فلترة القوائم/التقارير حسب الحالة
                e.HasIndex(d => d.IsActive);
            });

            // ===== Categories =====
            modelBuilder.Entity<Category>(e =>
            {
                // فهرس فريد على CategoryName -> يمنع التكرار ويحول البحث إلى O(log n) Index Seek
                e.HasIndex(c => c.CategoryName).IsUnique();
                // فهرس على IsActive لتسريع فلترة القوائم/التقارير حسب الحالة
                e.HasIndex(c => c.IsActive);
            });

            // ===== Products =====
            modelBuilder.Entity<Product>(e =>
            {
                // اسم الصنف فريد ضمن نفس التصنيف فقط (وليس عالمياً)
                e.HasIndex(p => new { p.CategoryId, p.ProductName }).IsUnique();
                // الباركود فريد إن وُجد (SQL Server يسمح بأكثر من NULL ضمن فهرس فريد)
                e.HasIndex(p => p.Barcode).IsUnique();
                e.HasIndex(p => p.IsActive);

                e.HasOne(p => p.Category)
                 .WithMany()
                 .HasForeignKey(p => p.CategoryId)
                 .OnDelete(DeleteBehavior.Restrict); // لا يسمح بحذف تصنيف مرتبط بأصناف (أساساً لا يوجد حذف فعلي)
            });

            // ===== Purchase Invoices =====
            modelBuilder.Entity<PurchaseInvoice>(e =>
            {
                e.HasIndex(i => i.InvoiceNumber).IsUnique();
                e.HasIndex(i => i.Status);
                e.HasIndex(i => i.InvoiceDate);
            });

            // ===== Old Invoice Archive (موديول أرشيف منفصل تماماً - لا علاقة له بـ PurchaseInvoices) =====
            modelBuilder.Entity<OldInvoiceArchive>(e =>
            {
                e.HasIndex(f => f.UploadedDate);
            });

            modelBuilder.Entity<PurchaseInvoiceDetail>(e =>
            {
                e.HasIndex(d => d.PurchaseInvoiceId);
                e.HasIndex(d => d.ProductId);

                e.HasOne(d => d.PurchaseInvoice)
                 .WithMany(i => i.Details)
                 .HasForeignKey(d => d.PurchaseInvoiceId)
                 .OnDelete(DeleteBehavior.Cascade);

                e.HasOne(d => d.Product)
                 .WithMany()
                 .HasForeignKey(d => d.ProductId)
                 .OnDelete(DeleteBehavior.Restrict); // لا يسمح بحذف صنف له حركات شراء سابقة
            });
            modelBuilder.Entity<Category>()
    .HasQueryFilter(c => !c.IsDeleted);

            // ===== Issues (سندات الصرف) =====
            modelBuilder.Entity<Issue>(e =>
            {
                e.HasIndex(i => i.IssueNumber).IsUnique();
                e.HasIndex(i => i.Status);
                e.HasIndex(i => i.IssueDate);
                e.HasIndex(i => i.DepartmentId);

                e.HasOne(i => i.Department)
                 .WithMany()
                 .HasForeignKey(i => i.DepartmentId)
                 .OnDelete(DeleteBehavior.Restrict); // لا يسمح بحذف قسم له سندات صرف
            });

            modelBuilder.Entity<IssueDetail>(e =>
            {
                e.HasIndex(d => d.IssueId);
                e.HasIndex(d => d.ProductId);

                e.HasOne(d => d.Issue)
                 .WithMany(i => i.Details)
                 .HasForeignKey(d => d.IssueId)
                 .OnDelete(DeleteBehavior.Cascade);

                e.HasOne(d => d.Product)
                 .WithMany()
                 .HasForeignKey(d => d.ProductId)
                 .OnDelete(DeleteBehavior.Restrict); // لا يسمح بحذف صنف له حركات صرف سابقة
            });

            // ===== Stock Take (الجرد) =====
            modelBuilder.Entity<StockTake>(e =>
            {
                e.HasIndex(s => s.Status);
                e.HasIndex(s => s.StockTakeDate);
            });

            modelBuilder.Entity<StockTakeDetail>(e =>
            {
                e.HasIndex(d => d.StockTakeId);
                e.HasIndex(d => d.ProductId);

                e.HasOne(d => d.StockTake)
                 .WithMany(s => s.Details)
                 .HasForeignKey(d => d.StockTakeId)
                 .OnDelete(DeleteBehavior.Cascade);

                e.HasOne(d => d.Product)
                 .WithMany()
                 .HasForeignKey(d => d.ProductId)
                 .OnDelete(DeleteBehavior.Restrict); // لا يسمح بحذف صنف له سطور جرد سابقة
            });

            // ===== Stock Transactions =====
            modelBuilder.Entity<StockTransaction>(e =>
            {
                // فهرس على الصنف لتسريع كشف حركة أي صنف (كشف حساب مخزني) O(log n)
                e.HasIndex(s => s.ProductId);
                e.HasIndex(s => s.TransactionDate);
                e.HasIndex(s => new { s.ReferenceType, s.ReferenceId });

                e.HasOne(s => s.Product)
                 .WithMany()
                 .HasForeignKey(s => s.ProductId)
                 .OnDelete(DeleteBehavior.Restrict);
            });
        }
    }
}