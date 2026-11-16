using System.ComponentModel.DataAnnotations;

namespace AccountingSyncAPP.Data.Dtos
{
    public class AccountStatementRequestDto
    {
        [Required]
        public string AccountId { get; set; } // رقم الحساب المطلوب

        [Required]
        public int CurrencyId { get; set; }   // رقم العملة

        [Required]
        public DateTime FromDate { get; set; } // من تاريخ

        [Required]
        public DateTime ToDate { get; set; }   // إلى تاريخ

        // --- حقول ضرورية لتحديد الجدول الديناميكي ---

        [Required]
        public string Year { get; set; }      // السنة (مثلاً "2024")

        [Required]
        public int BranchId { get; set; }     // رقم الفرع (لتحديد اللاحقة الصحيحة للجدول)

        // NEW: nullable — existing callers that omit it receive null (no filter applied)
        public int? DepartmentId { get; set; }
    }
}