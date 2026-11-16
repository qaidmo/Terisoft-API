using System;
using System.ComponentModel.DataAnnotations;

namespace AccountingSyncAPP.Data.Dtos
{
    /// <summary>
    /// موديل طلب تقرير حركة الأصناف - مستقل تماماً
    /// </summary>
    public class ItemMovementRequestDto
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

        // --- خيارات الفلترة ---

        /// <summary>
        /// نوع الحركة: "all" لكل الأنواع، أو كود محدد مثل "601"
        /// </summary>
        public string TransactionType { get; set; } = "all";

        /// <summary>
        /// رقم المستخدم للفلترة - null للأدمن (يرى الكل)
        /// </summary>
        public int? UserId { get; set; }

        /// <summary>
        /// رقم القسم للفلترة - null للأدمن
        /// </summary>
        public int? DepartmentId { get; set; }

        /// <summary>
        /// رقم مجموعة الأصناف للفلترة - null لكل المجموعات
        /// يقابل ITMGRPID في جدول SUNITEMGROUP
        /// </summary>
        public string? ItemGroupId { get; set; }
    }
}
