using System;
using System.Text.Json.Serialization;

namespace AccountingSyncAPP.Data.Models
{
    public class SupabaseAccountCurrency
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("account_id")]
        public string? AccountId { get; set; }

        [JsonPropertyName("currency_id")]
        public int CurrencyId { get; set; }
    }
}