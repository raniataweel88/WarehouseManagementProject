using Microsoft.EntityFrameworkCore;
using WarehouseUserManagement.Models;
using WarehouseUserManagement.Services;

namespace WarehouseUserManagement.Data
{
    /// <summary>يزرع الأدوار والصلاحيات الأساسية ومستخدم Admin الافتراضي عند أول تشغيل.</summary>
    public static class DbInitializer
    {
        public static void Seed(AppDbContext context, IPasswordHasher passwordHasher)
        {
            context.Database.Migrate();

            var permissions = new List<Permission>
            {
                // المستخدمون
                new() { PermissionName = "View Users",   ModuleName = "Users" },
                new() { PermissionName = "Add User",     ModuleName = "Users" },
                new() { PermissionName = "Edit User",    ModuleName = "Users" },
                new() { PermissionName = "Disable User", ModuleName = "Users" },
                new() { PermissionName = "Reset Password", ModuleName = "Users" },

                // الأدوار والصلاحيات
                new() { PermissionName = "Manage Roles",       ModuleName = "Roles" },
                new() { PermissionName = "Manage Permissions",  ModuleName = "Permissions" },

                // الأقسام
                new() { PermissionName = "View Departments",       ModuleName = "Departments" },
                new() { PermissionName = "Add Department",         ModuleName = "Departments" },
                new() { PermissionName = "Edit Department",        ModuleName = "Departments" },
                new() { PermissionName = "Deactivate Department",  ModuleName = "Departments" },

                // التصنيفات
                new() { PermissionName = "View Category",       ModuleName = "Categories" },
                new() { PermissionName = "Add Category",        ModuleName = "Categories" },
                new() { PermissionName = "Edit Category",       ModuleName = "Categories" },
                new() { PermissionName = "Deactivate Category", ModuleName = "Categories" },
                new() { PermissionName = "Delete Category",     ModuleName = "Categories" },
                new() { PermissionName = "Restore Category",    ModuleName = "Categories" },

                // الأصناف
                new() { PermissionName = "View Product",    ModuleName = "Products" },
                new() { PermissionName = "Add Product",     ModuleName = "Products" },
                new() { PermissionName = "Edit Product",    ModuleName = "Products" },
                new() { PermissionName = "Disable Product", ModuleName = "Products" },

                // فواتير الشراء
                new() { PermissionName = "View Purchase Invoices",  ModuleName = "Purchase" },
                new() { PermissionName = "Create Purchase Invoice", ModuleName = "Purchase" },
                new() { PermissionName = "Edit Purchase Invoice",   ModuleName = "Purchase" },
                new() { PermissionName = "Post Purchase Invoice",   ModuleName = "Purchase" },
                new() { PermissionName = "Cancel Purchase Invoice", ModuleName = "Purchase" },

                // الصرف
                new() { PermissionName = "View Issues",  ModuleName = "Issue" },
                new() { PermissionName = "Create Issue", ModuleName = "Issue" },
                new() { PermissionName = "Edit Issue",   ModuleName = "Issue" },
                new() { PermissionName = "Post Issue",   ModuleName = "Issue" },
                new() { PermissionName = "Cancel Issue", ModuleName = "Issue" },

                new() { PermissionName = "View StockTake",   ModuleName = "StockTake" },
                new() { PermissionName = "Create StockTake", ModuleName = "StockTake" },
                new() { PermissionName = "Edit StockTake",   ModuleName = "StockTake" },
                new() { PermissionName = "Post StockTake",   ModuleName = "StockTake" },

                // التقارير
                new() { PermissionName = "View Inventory Report", ModuleName = "Reports" },
                new() { PermissionName = "View Purchase Report",  ModuleName = "Reports" },
                new() { PermissionName = "View Issue Report",     ModuleName = "Reports" },
                new() { PermissionName = "View Audit Log",        ModuleName = "Reports" },
            };

            // Upsert: نضيف فقط الصلاحيات غير الموجودة أصلاً بدل تخطي الزرع بالكامل.
            // هذا يضمن أن أي صلاحية جديدة تُضاف بالكود (مثل Delete/Restore Category) تظهر
            // تلقائياً حتى على قاعدة بيانات شُغّلت من قبل، بدل أن تبقى غائبة عن شاشة الصلاحيات.
            var existingPermissionNames = context.Permissions
                .Select(p => p.PermissionName)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var newPermissions = permissions
                .Where(p => !existingPermissionNames.Contains(p.PermissionName))
                .ToList();

            if (newPermissions.Any())
            {
                context.Permissions.AddRange(newPermissions);
                context.SaveChanges();

                // نمنح الصلاحيات الجديدة تلقائياً لدور Admin فقط (باقي الأدوار تُدار يدوياً من شاشة الصلاحيات)
                var existingAdminRole = context.Roles.FirstOrDefault(r => r.RoleName == "Admin");
                if (existingAdminRole is not null)
                {
                    context.RolePermissions.AddRange(
                        newPermissions.Select(p => new RolePermission { RoleId = existingAdminRole.RoleId, PermissionId = p.PermissionId }));
                    context.SaveChanges();
                }
            }

            // زرع الأدوار + مستخدم Admin الافتراضي لمرة واحدة فقط (أول تشغيل على قاعدة بيانات فارغة)
            if (context.Roles.Any())
                return;

            // إعادة تحميل كل الصلاحيات (بما فيها المزروعة للتو) لضبط RoleId على كائنات كاملة بمعرّفاتها
            permissions = context.Permissions.ToList();

            var adminRole = new Role { RoleName = "Admin", Description = "مدير النظام - كل الصلاحيات", IsActive = true };
            var keeperRole = new Role { RoleName = "Warehouse Keeper", Description = "أمين المستودع", IsActive = true };
            var accountantRole = new Role { RoleName = "Accountant", Description = "المحاسب", IsActive = true };
            context.Roles.AddRange(adminRole, keeperRole, accountantRole);
            context.SaveChanges();

            // Admin: كل الصلاحيات
            context.RolePermissions.AddRange(
                permissions.Select(p => new RolePermission { RoleId = adminRole.RoleId, PermissionId = p.PermissionId }));

            // Warehouse Keeper
            var keeperPermNames = new[]
            {
                "View Departments",
                "View Category", "Add Category",
                "View Product", "Add Product", "Edit Product",
                "View Purchase Invoices", "Create Purchase Invoice", "Edit Purchase Invoice", "Post Purchase Invoice",
                "View Issues", "Create Issue", "Edit Issue", "Post Issue",
                "View StockTake", "Create StockTake", "Edit StockTake", "Post StockTake",
                "View Inventory Report"
            };
            context.RolePermissions.AddRange(
                permissions.Where(p => keeperPermNames.Contains(p.PermissionName))
                           .Select(p => new RolePermission { RoleId = keeperRole.RoleId, PermissionId = p.PermissionId }));

            // Accountant
            var accountantPermNames = new[]
            {
                "View Product", "View Purchase Invoices", "View Issues", "View StockTake",
                "View Purchase Report", "View Inventory Report"
            };
            context.RolePermissions.AddRange(
                permissions.Where(p => accountantPermNames.Contains(p.PermissionName))
                           .Select(p => new RolePermission { RoleId = accountantRole.RoleId, PermissionId = p.PermissionId }));

            context.SaveChanges();

            // مستخدم Admin افتراضي - يجب تغيير كلمة المرور فور أول دخول
            var adminUser = new User
            {
                FullName = "System Administrator",
                Username = "admin",
                PasswordHash = passwordHasher.HashPassword("Admin@123"),
                RoleId = adminRole.RoleId,
                Email = "admin@company.com",
                IsActive = true,
                CreatedBy = "System",
                CreatedDate = DateTime.UtcNow
            };
            context.Users.Add(adminUser);
            context.SaveChanges();
        }
    }
}