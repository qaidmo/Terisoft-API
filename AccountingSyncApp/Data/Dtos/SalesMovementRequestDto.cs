using System;
using System.ComponentModel.DataAnnotations;

namespace AccountingSyncAPP.Data.Dtos
{
    /// <summary>
    /// موديل طلب تقرير حركة المبيعات الجديد (مستقل تماماً عن Transaction History)
    /// </summary>
    public class SalesMovementRequestDto
    {
        [Required]
        public DateTime FromDate { get; set; }

        [Required]
        public DateTime ToDate { get; set; }

        /// <summary>
        /// السنة المالية (مثلاً "2024") لتحديد اللاحقة الصحيحة للجدول
        /// </summary>
        [Required]
        public string Year { get; set; }

        /// <summary>
        /// رقم الفرع - إلزامي دائماً لتحديد الجدول الصحيح وضمان نطاق البيانات الآمن
        /// </summary>
        [Required]
        public int BranchId { get; set; }

        /// <summary>
        /// نوع الحركة: "all" لكل الأنواع، أو كود محدد مثل "ن" (نقدي)، "ج" (آجل)، إلخ
        /// </summary>
        public string TransactionType { get; set; } = "all";

        /// <summary>
        /// رقم المستخدم للفلترة - null للأدمن (يرى الكل)، قيمة للمستخدم العادي (يرى حركاته فقط)
        /// </summary>
        public int? UserId { get; set; }

        /// <summary>
        /// رقم القسم للفلترة وضمان النطاق الآمن - null للأدمن، قيمة للمستخدم العادي
        /// </summary>
        public int? DepartmentId { get; set; }
    }
}
