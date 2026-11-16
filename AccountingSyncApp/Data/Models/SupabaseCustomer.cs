using System;
using System.Text.Json.Serialization; // 👈 المكتبة المعتمدة

namespace AccountingSyncAPP.Data.Models
{
    public class SupabaseCustomer
    {
        [JsonPropertyName("customer_no")]
        public int CustomerNo { get; set; }

        [JsonPropertyName("customer_name")]
        public string? CustomerName { get; set; }

        [JsonPropertyName("customer_foreign_name")]
        public string? CustomerForeignName { get; set; }

        [JsonPropertyName("phone_number")]
        public string? PhoneNumber { get; set; }

        [JsonPropertyName("address")]
        public string? Address { get; set; }

        [JsonPropertyName("email")]
        public string? Email { get; set; }

        [JsonPropertyName("account_id")]
        public string? AccountId { get; set; }

        [JsonPropertyName("source_id")]
        public string? SourceId { get; set; }

        [JsonPropertyName("control_account_id")]
        public string? ControlAccountId { get; set; }
        [JsonPropertyName("last_updated")]
        public string? LastUpdated { get; set; }
    }
}