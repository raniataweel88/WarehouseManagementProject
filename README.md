# نظام إدارة المستخدمين والصلاحيات — Warehouse User Management

تطبيق **ASP.NET Core MVC (C#)** كامل الـ Backend + Frontend، مبني على أساس وثيقة التحليل
"Module 1: إدارة المستخدمين والصلاحيات"، باستخدام **SQL Server** و **Entity Framework Core**.

---

## 1. المتطلبات (Prerequisites)

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- SQL Server (LocalDB / Express / Full) — أو عدّل `ConnectionStrings:DefaultConnection` في
  `WarehouseUserManagement/appsettings.json` ليطابق السيرفر عندك.
- (اختياري) Visual Studio 2022 أو VS Code + C# Dev Kit.

---

## 2. طريقة التشغيل

```bash
cd WarehouseUserManagement
dotnet restore
dotnet ef database update   # ينشئ قاعدة البيانات (أو تُنشأ تلقائياً عبر EnsureCreated عند أول تشغيل)
dotnet run
```

> ملاحظة: المشروع يستخدم `context.Database.EnsureCreated()` عند بدء التشغيل (`DbInitializer.Seed`)
> فينشئ الجداول ويزرع البيانات الأساسية تلقائياً من أول تشغيل بدون الحاجة لـ Migrations،
> وهذا مناسب للتجربة السريعة. للإنتاج الفعلي يُفضّل استخدام EF Core Migrations بدلاً من ذلك:
>
> ```bash
> dotnet ef migrations add InitialCreate
> dotnet ef database update
> ```
> (وعندها احذف استدعاء `EnsureCreated()` من `DbInitializer.cs` لتفادي التعارض مع الـ Migrations)

بعد التشغيل، افتح المتصفح على الرابط الذي يظهر في الطرفية (عادة `https://localhost:5001` أو ما شابه).

### بيانات الدخول الافتراضية

| Username | Password   |
|----------|-----------|
| `admin`  | `Admin@123` |

**غيّر كلمة المرور فوراً بعد أول دخول من صفحة "إعادة تعيين كلمة المرور".**

---

## 3. البنية (MVC Architecture)

```
WarehouseUserManagement/
├── Models/                  # الكيانات (User, Role, Permission, RolePermission, AuditLog)
│   └── ViewModels/          # نماذج العرض (Login, UserForm, RoleForm, ...)
├── Data/
│   ├── AppDbContext.cs      # EF Core + الفهارس (Indexes)
│   └── DbInitializer.cs     # زرع البيانات الأساسية
├── Services/
│   ├── PasswordHasher.cs    # PBKDF2-SHA256 (بدون مكتبات خارجية)
│   ├── PermissionService.cs # كاش صلاحيات كل دور (IMemoryCache)
│   ├── AuditService.cs      # تسجيل العمليات الحساسة
│   └── CurrentUserService.cs
├── Authorization/
│   └── HasPermissionAttribute.cs  # [HasPermission("Add User")] فحص O(1) عبر Claims
├── Controllers/              # Account, Users, Roles, Permissions, AuditLogs, Home
├── Views/                     # Razor Views (RTL, عربي)
└── wwwroot/css/site.css       # تصميم مخصص خفيف بدون مكتبات خارجية لأفضل أداء تحميل
```

---

## 4. قرارات الأداء و تعقيد الخوارزميات (Performance & Big-O Notes)

| الجزء | القرار | التعقيد الزمني |
|---|---|---|
| تسجيل الدخول / البحث باسم المستخدم | فهرس فريد (Unique Index) على `Username` | O(log n) Index Seek بدل O(n) Scan |
| فحص الصلاحيات في كل طلب (`[HasPermission]`) | الصلاحيات تُحمَّل مرة واحدة كـ Claims داخل الكوكي عند تسجيل الدخول، والفحص عبارة عن `HashSet.Contains` | **O(1)** بدون أي استعلام لقاعدة البيانات |
| صلاحيات الدور (Role → Permissions) | تُخزَّن مؤقتاً في الذاكرة (`IMemoryCache`) وتُفرّغ (Invalidate) فور تعديل صلاحيات الدور | O(1) بعد أول تحميل |
| كلمة المرور (Hash/Verify) | PBKDF2 بعدد تكرار ثابت (100,000) بغض النظر عن طول كلمة المرور | O(1) بالنسبة لطول الإدخال (تكلفة ثابتة مقصودة أمنياً) |
| قوائم المستخدمين / سجل العمليات | Paging على مستوى SQL Server (`Skip/Take` → `OFFSET/FETCH`) مع ترتيب على عمود مفهرس | O(log n + PageSize) بدل تحميل الجدول كاملاً |
| البحث بالاسم | `StartsWith` بدل `Contains` عندما يكون ذلك ممكناً | يسمح لـ SQL Server باستخدام Index Seek بدل Full Scan |
| القراءات (GET) | `AsNoTracking()` في كل الاستعلامات التي لا تحتاج تعديل | يقلل عبء EF Core في تتبع التغييرات |
| ربط صلاحيات دور جديد | إدراج/حذف بالجملة (`AddRange` / `RemoveRange`) بدل استعلام لكل صلاحية | O(k) حيث k = عدد الصلاحيات المختارة، بدل O(k) استعلامات منفصلة لكل واحدة |
| قوائم Roles مع عدد المستخدمين/الصلاحيات | استعلام واحد بـ `Select` يحسب `Count()` بدل حلقة + استعلام لكل دور | يتجنب مشكلة N+1 Query |

---

## 5. الأمان (Security)

- كلمات المرور تُخزَّن كـ Hash فقط (PBKDF2-SHA256 + Salt عشوائي، مقارنة بزمن ثابت).
- قفل الحساب (`IsLocked`) تلقائياً بعد عدد محاولات دخول خاطئة (افتراضياً 5، قابل للتعديل من `appsettings.json`).
- الجلسات (Cookies) بـ `HttpOnly` + `SameSite=Lax` + انتهاء صلاحية قابل للتمديد (Sliding Expiration).
- كل عملية إنشاء/تعديل/تفعيل/إيقاف/إعادة تعيين كلمة مرور تُسجَّل في `AuditLogs`.
- لا يوجد حذف فعلي للمستخدمين — فقط تعطيل (Business Rule من وثيقة التحليل).
- التحقق من صحة البيانات (Username فريد، كلمة مرور معقدة، بريد إلكتروني، إلخ) على مستوى
  الـ ViewModel (Data Annotations) وعلى مستوى قاعدة البيانات (Unique Indexes).

---

## 6. نقاط يمكن تطويرها لاحقاً

- إضافة Two-Factor Authentication.
- إضافة API منفصل (ASP.NET Core Web API) لو أردت لاحقاً واجهة SPA/Mobile.
- ربط الوحدة بباقي وحدات نظام المستودعات (الأصناف، فواتير الشراء، الصرف، التقارير) والتي
  ذُكرت الصلاحيات الخاصة بها في هذه الوحدة كتحضير مسبق.
