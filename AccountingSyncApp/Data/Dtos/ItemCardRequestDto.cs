using System;
using System.ComponentModel.DataAnnotations;

namespace AccountingSyncAPP.Data.Dtos
{
    /// <summary>
    /// موديل طلب تقرير بطاقة الصنف - مستقل تماماً
    /// </summary>
    public class ItemCardRequestDto
    {
        // --- نطاق التاريخ (إجباري) ---
        [Required]
        public DateTime FromDate { get; set; }

        [Required]
        public DateTime ToDate { get; set; }

        // --- سياق قاعدة البيانات ---

        /// <summary>
        /// السنة المالية (مثلاً "2024") لتحديد اللاحقة الصحيحة للجدول
        /// </summary>
        [Required]
        public string Year { get; set; }

        /// <summary>
        /// رقم الفرع - إلزامي دائماً لتحديد الجدول الصحيح
        /// </summary>
        [Required]
        public int BranchId { get; set; }

        // --- فلاتر اختيارية ---

        /// <summary>
        /// اسم الصنف — بحث جزئي LIKE على d.TRADITMNAME
        /// </summary>
        public string? ItemName { get; set; }

        /// <summary>
        /// اسم الوحدة — بحث تام على d.TRADITMUNIT
        /// </summary>
        public string? UnitName { get; set; }

        /// <summary>
        /// رقم الدفعة — بحث تام على d.TRADREFID
        /// </summary>
        public string? BatchNumber { get; set; }

        /// <summary>
        /// نوع الحركة: "all" لكل الأنواع، أو كود محدد مثل "601"
        /// </summary>
        public string TransactionType { get; set; } = "all";

        // --- إعدادات ---
        public bool ExcludeOpeningBalance  { get; set; } = false;
        public bool ExcludePreviousBalance { get; set; } = false;

        // --- أمان النطاق ---

        /// <summary>
        /// رقم المستخدم للفلترة - null للأدمن (يرى الكل)
        /// </summary>
        public int? UserId { get; set; }

        /// <summary>
        /// رقم القسم للفلترة - null للأدمن
        /// </summary>
        public int? DepartmentId { get; set; }
    }
}
