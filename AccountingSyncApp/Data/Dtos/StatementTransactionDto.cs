namespace AccountingSyncAPP.Data.Dtos
{
    public class AccountStatementResponseDto
    {
        // مجاميع الفترة فقط (اختياري، مفيد للعرض السريع)
        public decimal TotalPeriodDebit { get; set; }
        public decimal TotalPeriodCredit { get; set; }

        public decimal OpeningBalance { get; set; }
        public decimal ClosingBalance { get; set; }

        public string? LedgerSignature { get; set; }

        public List<StatementTransactionDto> Transactions { get; set; }

        public AccountStatementResponseDto()
        {
            Transactions = new List<StatementTransactionDto>();
        }
    }
}