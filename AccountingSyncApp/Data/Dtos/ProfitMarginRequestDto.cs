using System;
using System.ComponentModel.DataAnnotations;

namespace AccountingSyncAPP.Data.Dtos
{
    /// <summary>
    /// موديل طلب تقرير هامش الربح — مستقل تماماً
    /// يدعم التجميع الديناميكي حسب الصنف / المجموعة / المستخدم
    /// </summary>
    public class ProfitMarginRequestDto
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

        /// <summary>رقم الفرع — إلزامي دائماً لتحديد الجدول ونطاق البيانات</summary>
        [Required]
        public int BranchId { get; set; }

        // --- خيارات التجميع ---

        /// <summary>
        /// محور التجميع:
        ///   "item"  — تجميع حسب الصنف (افتراضي)
        ///   "group" — تجميع حسب مجموعة الأصناف
        ///   "user"  — تجميع حسب المستخدم
        /// </summary>
        public string GroupBy { get; set; } = "item";

        // --- خيارات الفلترة ---

        /// <summary>رقم المستخدم للفلترة — null للأدمن (يرى الكل)</summary>
        public int? UserId { get; set; }

        /// <summary>رقم القسم للفلترة — null للأدمن</summary>
        public int? DepartmentId { get; set; }

        /// <summary>رقم مجموعة الأصناف — null لكل المجموعات</summary>
        public string? ItemGroupId { get; set; }

        /// <summary>إذا true يُرجع فقط الصفوف ذات الهامش السالب</summary>
        public bool NegativeMarginOnly { get; set; } = false;

        // --- أعمدة اختيارية (تُضاف للـ SELECT عند الحاجة) ---
        public bool IncludeBranch     { get; set; } = false;
        public bool IncludeDepartment { get; set; } = false;
        public bool IncludeBatch      { get; set; } = false;
        public bool IncludeCostCenter { get; set; } = false;
    }
}
