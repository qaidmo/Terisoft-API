using System.Text.Json.Serialization;

namespace AccountingSyncAPP.Data.Models
{
    public class SupabaseCurrency
    {
        [JsonPropertyName("currency_no")]
        public int CurrencyNo { get; set; }

        [JsonPropertyName("currency_name")]
        public string? CurrencyName { get; set; }

        [JsonPropertyName("currency_symbol")]
        public string? CurrencySymbol { get; set; }

        [JsonPropertyName("currency_fkah")]
        public string? CurrencyFkah { get; set; } 

        [JsonPropertyName("currency_rate")]
        public decimal CurrencyRate { get; set; }

        [JsonPropertyName("currency_highest")]
        public decimal CurrencyHighest { get; set; }

        [JsonPropertyName("currency_lowest")]
        public decimal CurrencyLowest { get; set; }

        [JsonPropertyName("source_id")]
        public string? SourceId { get; set; }
    }
}