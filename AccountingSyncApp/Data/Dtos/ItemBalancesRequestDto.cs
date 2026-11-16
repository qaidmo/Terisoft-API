using System;
using System.ComponentModel.DataAnnotations;

namespace AccountingSyncAPP.Data.Dtos
{
    /// <summary>
    /// موديل طلب تقرير أرصدة الأصناف — مستقل تماماً
    /// يدعم التجميع الديناميكي وفلترة الأرصدة
    /// </summary>
    public class ItemBalancesRequestDto
    {
        // --- نطاق التاريخ (إجباري) ---
        [Required]
        public DateTime FromDate { get; set; }

        [Required]
        public DateTime ToDate { get; set; }

        // --- سياق قاعدة البيانات ---
        /// <summary>السنة المالية (مثلاً "2024") لتحديد اللاحقة الصحيحة للجدول</summary>
        [Required]
        public string Year { get; set; }

        /// <summary>رقم الفرع — إلزامي لتحديد اسم الجدول</summary>
        [Required]
        public int BranchId { get; set; }

        // --- أمان نطاق البيانات ---
        /// <summary>رقم المستخدم — null للأدمن</summary>
        public int? UserId { get; set; }

        /// <summary>رقم القسم — null للأدمن</summary>
        public int? DepartmentId { get; set; }

        // --- أعمدة اختيارية تُضاف للـ GROUP BY والـ SELECT ---
        /// <summary>إضافة عمود الفرع</summary>
        public bool IncludeBranch { get; set; } = false;

        /// <summary>إضافة عمود القسم</summary>
        public bool IncludeDepartment { get; set; } = false;

        /// <summary>
        /// إضافة عمودَي المخزن (رقم + اسم) بعد سعر البيع
        /// وعمود "رصيد الصنف" في نهاية الجدول
        /// </summary>
        public bool IncludeStore { get; set; } = false;

        /// <summary>إضافة عمود تاريخ الانتهاء</summary>
        public bool IncludeExpiry { get; set; } = false;

        /// <summary>إضافة عمود رقم الدفعة</summary>
        public bool IncludeBatch { get; set; } = false;

        // --- فلاتر جانب السيرفر ---
        /// <summary>استبعاد الصفوف التي رصيدها = صفر</summary>
        public bool ExcludeZeros { get; set; } = false;

        /// <summary>إظهار الأصناف ذات الكميات السالبة فقط</summary>
        public bool NegativeQtyOnly { get; set; } = false;

        /// <summary>
        /// استبعاد الرصيد السابق (يصفّر الكمية الافتتاحية قبل حساب الكمية النهائية)
        /// </summary>
        public bool ExcludePreviousBalance { get; set; } = false;

        /// <summary>إظهار الأصناف ذات الرصيد = صفر فقط</summary>
        public bool ZeroBalancesOnly { get; set; } = false;
    }
}
