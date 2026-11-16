using System;
using System.ComponentModel.DataAnnotations;

namespace AccountingSyncAPP.Data.Dtos
{
    /// <summary>
    /// موديل طلب سجل الحركات (المبيعات والسندات)
    /// يستخدم لنقل فلاتر البحث من التطبيق إلى السيرفر
    /// </summary>
    public class TransactionHistoryRequestDto
    {
        // --- 1. نطاق التاريخ (إجباري) ---
        [Required]
        public DateTime FromDate { get; set; } // بداية الفترة

        [Required]
        public DateTime ToDate { get; set; }   // نهاية الفترة

        // --- 2. سياق قاعدة البيانات (لتحديد الجدول الديناميكي) ---
        [Required]
        public string Year { get; set; }       // السنة المالية (مثلاً "2024") لتحديد جزء من اسم الجدول

        [Required]
        public int BranchId { get; set; }      // رقم الفرع لتحديد اللاحقة الصحيحة للجدول (مثل SunTransHeadline20241)


        // --- 3. خيارات الفلترة (المنطق الذي اتفقنا عليه) ---

        /// <summary>
        /// نوع الحركة المطلوب فلترته.
        /// يمكن أن يحمل قيمة كود محدد مثل "601" (نقدي)، "201" (قبض).
        /// أو يحمل القيمة "all" لجلب كل الأنواع (مبيعات + مردودات + سندات).
        /// </summary>
        public string TransactionType { get; set; }

        /// <summary>
        /// رقم المستخدم للطالب.
        /// - إذا كان الطالب (Admin): يمكن إرسال null أو 0 لرؤية حركات الجميع.
        /// - إذا كان الطالب (مستخدم عادي): يجب إرسال رقم المستخدم لفلترة الحركات الخاصة به فقط (TrauId).
        /// </summary>
        public int? UserId { get; set; }

        /// <summary>
        /// رقم القسم للفلترة (TraSecId).
        /// يمكن أن يكون null إذا كان المستخدم أدمن ويريد رؤية كل الأقسام.
        /// </summary>
        public int? DepartmentId { get; set; }
    }
}