using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace AccountingSyncAPP.Data.Dtos
{
    public class CostCenterReportResponseDto
    {
        [JsonPropertyName("transactions")]
        public List<CostCenterTransactionDto> Transactions { get; set; } = new List<CostCenterTransactionDto>();
    }

    public class CostCenterTransactionDto
    {
        [JsonPropertyName("transaction_date")]
        public string TransactionDate { get; set; }

        [JsonPropertyName("transaction_ref")]
        public string TransactionRef { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; }

        [JsonPropertyName("transaction_type")]
        public string TransactionType { get; set; }

        [JsonPropertyName("account_number")]
        public string AccountNumber { get; set; }

        [JsonPropertyName("account_name")]
        public string AccountName { get; set; }

        [JsonPropertyName("debit")]
        public decimal Debit { get; set; }

        [JsonPropertyName("credit")]
        public decimal Credit { get; set; }
    }
}
