using System.Text.Json.Serialization; // 👈 ضروري جداً

namespace AccountingSyncAPP.Data.Models
{
    public class SupabaseCustomerCurrency
    {
        [JsonPropertyName("customer_no")]
        public int CustomerNo { get; set; }

        [JsonPropertyName("currency_no")]
        public int CurrencyNo { get; set; }
    }
}