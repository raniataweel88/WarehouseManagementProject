// تأكيد قبل تنفيذ عمليات حساسة (تعطيل مستخدم، إلخ)
document.addEventListener('click', function (e) {
    const el = e.target.closest('[data-confirm]');
    if (el && !confirm(el.getAttribute('data-confirm'))) {
        e.preventDefault();
    }
});

// ===== سلوك موحّد لكل المودالات (Create / Edit) في التطبيق =====
// 1) عند فتح أي مودال: فوكس تلقائي على أول حقل إدخال قابل للكتابة فيه.
// 2) الضغط على Enter داخل حقل (غير textarea): ينتقل للحقل التالي بدل إرسال الفورم مباشرة،
//    وإذا كان آخر حقل في الفورم يقوم بإرسال الفورم (نفس سلوك زر الحفظ).
// يعمل هذا حتى مع المودالات التي تُحمَّل محتواها لاحقاً عبر Ajax (مثل مودال تعديل التصنيف/القسم/الصنف/المستخدم)
// لأن الاستماع للأحداث معرّف على مستوى document (Event Delegation) وليس على عناصر بعينها.

function getFocusableFormFields(container) {
    return Array.from(
        container.querySelectorAll('input, select, textarea')
    ).filter(function (el) {
        if (el.type === 'hidden' || el.disabled || el.readOnly) return false;
        if (el.offsetParent === null) return false; // مخفي (display:none) أو داخل تبويب غير ظاهر
        return true;
    });
}

document.addEventListener('shown.bs.modal', function (e) {
    var modal = e.target;
    var fields = getFocusableFormFields(modal);
    if (fields.length > 0) {
        fields[0].focus();
        if (typeof fields[0].select === 'function' && (fields[0].tagName === 'INPUT')) {
            fields[0].select();
        }
    }
});

// ===== إرسال فورمات التعديل داخل المودالات عبر Ajax =====
// المشكلة: فورم التعديل (مثلاً تعديل تصنيف) يتم تحميله داخل المودال عبر Ajax (GET)،
// لكن إرساله (POST) كان يحصل بشكل طبيعي (Full Page Navigation). لو فشل التحقق
// (مثلاً "اسم مستخدم مسبقاً")، كان السيرفر يرجّع نفس الفورم كـ Partial View بدون
// Layout، فتظهر صفحة عارية بدون أي تنسيق (CSS/Bootstrap) بدل البقاء داخل المودال.
// الحل: أي فورم عليه data-ajax-modal يتم اعتراض إرساله وإرساله عبر fetch:
//   - لو نجحت العملية، السيرفر يعمل Redirect (لصفحة القائمة) -> نتبع نفس الرابط لتحديث الصفحة.
//   - لو فشل التحقق، السيرفر يرجّع نفس الفورم (بدون Redirect) -> نستبدل محتوى المودال
//     بالنسخة الجديدة (فيها رسائل الأخطاء) ونتركه مفتوحاً، مع إعادة تفعيل الفوكس/Enter تلقائياً.
document.addEventListener('submit', function (e) {
    var form = e.target;
    if (!(form instanceof HTMLFormElement)) return;
    if (!form.hasAttribute('data-ajax-modal')) return;

    var modal = form.closest('.modal');
    if (!modal) return;

    e.preventDefault();

    var submitBtn = form.querySelector('button[type="submit"]');
    if (submitBtn) submitBtn.disabled = true;

    fetch(form.action, {
        method: (form.method || 'post').toUpperCase(),
        body: new FormData(form),
        credentials: 'same-origin',
        headers: { 'X-Requested-With': 'XMLHttpRequest' }
    })
        .then(function (response) {
            if (response.redirected) {
                // العملية نجحت (السيرفر عمل Redirect) -> نتبع نفس المسار لتحديث القائمة وعرض رسالة النجاح
                window.location.href = response.url;
                return null;
            }
            return response.text();
        })
        .then(function (html) {
            if (html === null || html === undefined) return;

            var container = modal.querySelector('.modal-content');
            if (container) {
                container.innerHTML = html;

                var fields = getFocusableFormFields(container);
                if (fields.length > 0) fields[0].focus();
            }
        })
        .catch(function () {
            if (submitBtn) submitBtn.disabled = false;
        });
});

document.addEventListener('keydown', function (e) {
    if (e.key !== 'Enter') return;

    var target = e.target;
    if (!target || target.tagName === 'TEXTAREA') return; // نسمح بالسطر الجديد داخل textarea

    var modal = target.closest('.modal');
    if (!modal) return;

    var form = target.closest('form');
    if (!form) return;

    // إذا كان العنصر نفسه زر (submit/button) اترك السلوك الافتراضي
    if (target.tagName === 'BUTTON' || target.type === 'submit') return;

    var fields = getFocusableFormFields(form);
    var index = fields.indexOf(target);
    if (index === -1) return;

    e.preventDefault();

    var next = fields[index + 1];
    if (next) {
        next.focus();
        if (typeof next.select === 'function' && next.tagName === 'INPUT') {
            next.select();
        }
    } else {
        // آخر حقل في الفورم -> إرسال الفورم (نفس زر الحفظ)
        if (typeof form.requestSubmit === 'function') {
            form.requestSubmit();
        } else {
            form.submit();
        }
    }
});
