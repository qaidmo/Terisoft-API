using System;
using System.Text.Json.Serialization;

namespace AccountingSyncAPP.Data.Models
{
    public class SupabaseReceiptPaymentItem
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("receiptPaymentId")]
        public long ReceiptPaymentId { get; set; }

        [JsonPropertyName("accountNo")]
        public string? AccountNo { get; set; }

        [JsonPropertyName("accountName")]
        public string? AccountName { get; set; }

        [JsonPropertyName("amount")]
        public decimal Amount { get; set; }

        [JsonPropertyName("itemCurrency")]
        public string? ItemCurrency { get; set; }

        [JsonPropertyName("itemCurrencyId")]
        public int ItemCurrencyId { get; set; }

        [JsonPropertyName("exchangeRate")]
        public decimal ExchangeRate { get; set; }

        [JsonPropertyName("equivalentAmount")]
        public decimal EquivalentAmount { get; set; }

        [JsonPropertyName("controlAccountId")]
        public string? ControlAccountId { get; set; }
    }
}