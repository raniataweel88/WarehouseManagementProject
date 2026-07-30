using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using System.Text.Encodings.Web;
using System.Text.Unicode;
using WarehouseUserManagement.Data;
using WarehouseUserManagement.Services;

var builder = WebApplication.CreateBuilder(args);

// ===== MVC =====
builder.Services.AddControllersWithViews();

// ===== Encoder =====
// افتراضياً HtmlEncoder في ASP.NET Core يسمح فقط بنطاق Basic Latin ويحوّل أي حرف عربي إلى
// HTML Entity رقمي (&#1610;...) عند طباعته بـ @Something. هذا يعمل بشكل طبيعي داخل HTML عادي
// (المتصفح يفكّه تلقائياً)، لكن داخل <script> المتصفح لا يفكّ هذه الـ Entities إطلاقاً،
// فكانت رسائل SweetAlert (مثل @TempData["Success"]) تظهر كرموز غريبة (&#1610;&#1605;...) بدل العربي.
// السماح بكامل مدى Unicode هنا يحل المشكلة على مستوى التطبيق بالكامل بدل تعديل كل صفحة.
builder.Services.AddSingleton(HtmlEncoder.Create(UnicodeRanges.All));

// ===== Database (EF Core + SQL Server) =====
// ملاحظة أداء: التتبع (Tracking) يبقى مفعّلاً افتراضياً لأن أكشنات التعديل (Edit/ToggleActive/ResetPassword)
// تعتمد عليه لاكتشاف التغييرات تلقائياً. بالمقابل، كل استعلامات القراءة فقط (Index/Details/GET)
// تستخدم AsNoTracking() صراحة في الـ Controllers لتقليل عبء EF Core عليها.
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// ===== Memory cache (يُستخدم من PermissionService لتخزين صلاحيات كل دور) =====
builder.Services.AddMemoryCache();

// ===== HttpContextAccessor (يُستخدم من CurrentUserService) =====
builder.Services.AddHttpContextAccessor();

// ===== Application services =====
builder.Services.AddScoped<IPasswordHasher, PasswordHasher>();
builder.Services.AddScoped<IPermissionService, PermissionService>();
builder.Services.AddScoped<IAuditService, AuditService>();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

// ===== Authentication (Cookie-based, صلاحيات محمّلة كـ Claims) =====
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromMinutes(
            builder.Configuration.GetValue<int?>("AppSettings:SessionTimeoutMinutes") ?? 30);
        options.SlidingExpiration = true; // تمديد الجلسة مع النشاط (Session Management - وثيقة التحليل قسم 9)
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
    });

builder.Services.AddAuthorization();

var app = builder.Build();

// ===== Seed database on startup =====
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
    DbInitializer.Seed(context, hasher);
}

// ===== HTTP pipeline =====
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
// ملفات ثابتة (CSS/JS) تُخدَّم مع Cache-Control قوي لتحسين الأداء على المتصفح
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}");

app.Run();