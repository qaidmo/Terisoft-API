using System;
using System.ComponentModel.DataAnnotations;

namespace AccountingSyncAPP.Data.Dtos
{
    /// <summary>
    /// موديل الطلب: يحدد الفترة والعملة والفرع لجلب ملخص حركة الموردين
    /// </summary>
    public class SupplierMovementRequestDto
    {
        [Required]
        public DateTime FromDate { get; set; }

        [Required]
        public DateTime ToDate { get; set; }

        // لتحديد الجدول الديناميكي (SunTransHeadline{Year}{BranchId})
        [Required]
        public string Year { get; set; }

        [Required]
        public int BranchId { get; set; }

        public int DepartmentId { get; set; }

        [Required]
        public int CurrencyId { get; set; }

        // للفلترة حسب صلاحيات المستخدم (اختياري)
        public int? UserId { get; set; }

        /// <summary>
        /// إذا كان true: يُفعِّل وضع التفصيلي — صف لكل فاتورة شراء مع قيمة المخزون.
        /// إذا كان false (الافتراضي): وضع الملخص — صف واحد لكل مورد.
        /// </summary>
        public bool IsDetailedMode { get; set; } = false;
    }
}
