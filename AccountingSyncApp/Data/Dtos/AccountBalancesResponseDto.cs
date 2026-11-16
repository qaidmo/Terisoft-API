using System.Collections.Generic;

namespace AccountingSyncAPP.Data.Dtos
{
    // =========================================================================
    // 1. موديل الصف الواحد في نتيجة التقرير
    // =========================================================================
    public class AccountBalancesRowDto
    {
        // --- أعمدة تعريف الكيان (تتغير حسب وضع التجميع) ---
        public string EntityId { get; set; } = "";
        public string EntityName { get; set; } = "";

        // --- أعمدة سياقية اختيارية (تُملأ فقط في الأوضاع المناسبة) ---
        public string AccountName { get; set; } = "";      // أوضاع: byTransactionName, byCostCenter, byBeneficiary, byCostCenterAndAccount
        public string BeneficiaryName { get; set; } = "";   // وضع: byBeneficiary فقط
        public string CostCenterName { get; set; } = "";    // وضع: byCostCenterAndAccount فقط

        // --- الأعمدة المالية ---
        public decimal OpeningDebit { get; set; }
        public decimal OpeningCredit { get; set; }
        public decimal PeriodDebit { get; set; }
        public decimal PeriodCredit { get; set; }
        public decimal TotalDebit { get; set; }     // = OpeningDebit + PeriodDebit
        public decimal TotalCredit { get; set; }    // = OpeningCredit + PeriodCredit
        public decimal BalanceDebit { get; set; }   // IF net > 0
        public decimal BalanceCredit { get; set; }  // IF net < 0
        public decimal NetBalance { get; set; }     // = TotalDebit - TotalCredit
        public decimal Value { get; set; }          // = |NetBalance|
    }

    // =========================================================================
    // 2. موديل الاستجابة الكاملة
    // =========================================================================
    public class AccountBalancesResponseDto
    {
        public List<AccountBalancesRowDto> Rows { get; set; } = new();
        public int RowCount { get; set; }

        // --- المجاميع الكلية ---
        public decimal GrandOpeningDebit { get; set; }
        public decimal GrandOpeningCredit { get; set; }
        public decimal GrandPeriodDebit { get; set; }
        public decimal GrandPeriodCredit { get; set; }
        public decimal GrandTotalDebit { get; set; }
        public decimal GrandTotalCredit { get; set; }
        public decimal GrandBalanceDebit { get; set; }
        public decimal GrandBalanceCredit { get; set; }
        public decimal GrandNetBalance { get; set; }
        public decimal GrandValue { get; set; }
    }

    // =========================================================================
    // 3. موديل خام لنتيجة SqlQueryRaw — يُطابق أعمدة الاستعلام
    // =========================================================================
    public class AccountBalancesRawRow
    {
        public string EntityId { get; set; } = "";
        public string EntityName { get; set; } = "";
        public string AccountName { get; set; } = "";
        public string BeneficiaryName { get; set; } = "";
        public string CostCenterName { get; set; } = "";

        public decimal OpeningDebit { get; set; }
        public decimal OpeningCredit { get; set; }
        public decimal PeriodDebit { get; set; }
        public decimal PeriodCredit { get; set; }
    }
}
