using System;
using System.Text.Json.Serialization; 

namespace AccountingSyncAPP.Data.Models
{
    public class SupabaseSalesperson
    {
        [JsonPropertyName("id")]
        public int? Id { get; set; }

        [JsonPropertyName("salesperson_no")]
        public int SalespersonNo { get; set; }

        [JsonPropertyName("salesperson_name")]
        public string? SalespersonName { get; set; }

        [JsonPropertyName("phone_number")]
        public string? PhoneNumber { get; set; }

        [JsonPropertyName("percentage")]
        public decimal? Percentage { get; set; }

        [JsonPropertyName("commissions_disabled")]
        public bool CommissionsDisabled { get; set; }

        [JsonPropertyName("account_id")]
        public string? AccountId { get; set; }

        [JsonPropertyName("expense_account_id")]
        public string? ExpenseAccountId { get; set; }

        [JsonPropertyName("source_id")]
        public string? SourceId { get; set; }
        [JsonPropertyName("last_updated")]
        public string? LastUpdated { get; set; }
    }
}