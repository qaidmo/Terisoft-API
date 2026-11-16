using System;
using System.ComponentModel.DataAnnotations;

namespace AccountingSyncAPP.Data.Dtos
{
    /// <summary>
    /// موديل طلب تقرير أرصدة الحسابات — مستقل تماماً
    /// يدعم 6 أوضاع تجميع ديناميكية وفلترة بالعملة
    /// </summary>
    public class AccountBalancesRequestDto
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

        // --- فلاتر اختيارية ---
        /// <summary>رقم القسم — null يعني بدون فلترة قسم</summary>
        public int? DepartmentId { get; set; }

        /// <summary>
        /// رقم العملة — 0 أو null يعني العملة المحلية (بدون فلتر TRADCURID)
        /// قيمة > 0 تعني فلترة بالعملة المحددة
        /// </summary>
        public int? CurrencyId { get; set; }

        /// <summary>
        /// وضع التجميع — أحد القيم التالية:
        /// "byMainAccount", "byTransactionName", "byCostCenter",
        /// "byBeneficiary", "byCostCenterAndAccount", "analyticalAll"
        /// </summary>
        [Required]
        public string GroupingMode { get; set; }
    }
}
