using System.Collections.Generic;

namespace AccountingSyncAPP.Data.Dtos
{
    /// <summary>
    /// موديل استجابة تقرير حركة المبيعات - يحتوي على البيانات الكاملة بما فيها الضريبة والخصم
    /// </summary>
    public class SalesMovementResponseDto
    {
        public int ItemsCount { get; set; }
        public decimal GrandNetValue { get; set; }
        public decimal GrandFinalValue { get; set; }
        public List<SalesMovementItemDto> Items { get; set; }
    }

    /// <summary>
    /// بيانات صف واحد في تقرير حركة المبيعات
    /// </summary>
    public class SalesMovementItemDto
    {
        public string TransactionId { get; set; }
        public string TransactionNumber { get; set; }
        public string Date { get; set; }
        public string Time { get; set; }
        public string TransactionTypeId { get; set; }
        public string TransactionTypeName { get; set; }

        /// <summary>
        /// اسم المستفيد (TraPatName)
        /// </summary>
        public string BeneficiaryName { get; set; }

        /// <summary>
        /// اسم الحساب من جدول SUNACCDIR (عبر TraPatId)
        /// </summary>
        public string AccountName { get; set; }

        public string UserName { get; set; }
        public string BranchName { get; set; }
        public string DepartmentName { get; set; }

        /// <summary>
        /// إجمالي الفاتورة قبل الخصم (TraGross)
        /// </summary>
        public decimal GrossTotal { get; set; }

        /// <summary>
        /// قيمة الخصم (TraDisc)
        /// </summary>
        public decimal DiscountAmount { get; set; }

        /// <summary>
        /// قيمة الضريبة (TraTax)
        /// </summary>
        public decimal TaxAmount { get; set; }

        /// <summary>
        /// صافي القيمة بعد الخصم وقبل الضريبة (TraNet)
        /// </summary>
        public decimal NetValue { get; set; }

        /// <summary>
        /// القيمة النهائية بعد الخصم والضريبة (TraTotal)
        /// </summary>
        public decimal FinalValue { get; set; }
    }
}
