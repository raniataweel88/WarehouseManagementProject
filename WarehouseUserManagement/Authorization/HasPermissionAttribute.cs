using Azure.Core;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Security.Claims;
using WarehouseUserManagement.Controllers;
using WarehouseUserManagement.Services;

namespace WarehouseUserManagement.Authorization
{
    /// <summary>
    /// [HasPermission("Add Product")] على أي Action/Controller.
    /// الفحص يتم بالاعتماد على IPermissionService (مخزّن مؤقتاً في الذاكرة ويُفرَّغ تلقائياً عند
    /// تعديل صلاحيات الدور من صفحة الأدوار)، وليس على Claims محمَّلة وقت تسجيل الدخول فقط.
    /// هذا يضمن أن أي تغيير على صلاحيات دور ما ينعكس فوراً على كل المستخدمين المرتبطين فيه،
    /// حتى لو كانت جلساتهم مفتوحة أصلاً (بدون الحاجة لتسجيل خروج/دخول من جديد).
    /// دور Admin يحمل Claim خاص "perm" = "*" فيتجاوز كل الفحوصات مباشرة.
    /// </summary>
    public class HasPermissionAttribute : TypeFilterAttribute
    {
        public HasPermissionAttribute(string permission) : base(typeof(PermissionAuthorizationFilter))
        {
            Arguments = new object[] { permission };
        }
    }

    public class PermissionAuthorizationFilter : IAsyncAuthorizationFilter
    {
        private const string PermissionClaimType = "perm";
        private const string WildcardPermission = "*";

        private readonly string _requiredPermission;
        private readonly IPermissionService _permissionService;

        public PermissionAuthorizationFilter(string requiredPermission, IPermissionService permissionService)
        {
            _requiredPermission = requiredPermission;
            _permissionService = permissionService;
        } /// <summary>
          ///ما هو AuthorizationFilterContext context؟
          /// هذا الكائن يحتوي على كل المعلومات الخاصة بالطلب الحالي.
          /// يمكنك من خلاله الوصول إلى:
          /// المستخدم الحالي.
          ///    الـ Request.
          ///     الـ Response.
          ///      عنوان الصفحة المطلوبة.
          ///   إيقاف تنفيذ الطلب.
          ///    إعادة التوجيه.
          ///    يمكن اعتباره صندوقًا يحتوي على كل بيانات الطلب.
          /// </summary>

        public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {

            //            HttpContext يمثل الاتصال الحالي بين المتصفح والخادم.
           //من خلاله تستطيع الوصول إلى:
            //            المستخدم.
            //            الكوكيز.
            //            الجلسة.
            //            عنوان الطلب.
            //الـ Request.
            //الـ Response.
            var user = context.HttpContext.User;

            if (user.Identity is null || !user.Identity.IsAuthenticated)
            {
                //هذا يحفظ الصفحة التي كان يريد المستخدم الوصول إليها.
                var returnUrl = context.HttpContext.Request.Path + context.HttpContext.Request.QueryString;
                //AccountController ثم إلى Login  أوقف تنفيذ الطلب، واذهب إلى 
                context.Result = new RedirectToActionResult("Login", "Account", new { returnUrl });
                return;
            }

            // Admin عنده Claim ثابت "*" (لا يتغيّر ولا يُخزَّن كصلاحيات دور عادية) -> يتجاوز الفحص مباشرة
            //الـ Claims هي معلومات مخزنة مع المستخدم بعد تسجيل الدخول.
            if (user.Claims.Any(c => c.Type == PermissionClaimType && c.Value == WildcardPermission))
                return;
            //هنا يبدأ بجلب RoleId من الـ Claims حتى يعرف دور المستخدم، وبعدها يستخدمه لقراءة الصلاحيات الخاصة بهذا الدور.
            var roleIdValue = user.FindFirst("RoleId")?.Value;
            if (!int.TryParse(roleIdValue, out var roleId))
            {
                //المستخدم معروف ومسجل دخول، لكن ليس لديه صلاحية للوصول.
                context.Result = new ForbidResult();
                return;
            }

            // نقرأ صلاحيات الدور الحالية من الكاش (يُفرَّغ تلقائياً عند تعديل الدور) بدل الاعتماد
            // على صلاحيات قديمة كانت محفوظة داخل الـ Cookie وقت تسجيل الدخول.
            var permissionSet = await _permissionService.GetPermissionNamesForRoleAsync(roleId);

            if (permissionSet.Contains(_requiredPermission))
                return;

            context.Result = new ForbidResult();
        }
    }
}
