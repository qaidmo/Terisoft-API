using System;
using System.Text.Json.Serialization; 

namespace AccountingSyncAPP.Data.Models
{
    public class SupabaseCollector
    {
        [JsonPropertyName("collector_no")]
        public int CollectorNo { get; set; }

        [JsonPropertyName("collector_name")]
        public string? CollectorName { get; set; }

        [JsonPropertyName("phone_number")]
        public string? PhoneNumber { get; set; }
        [JsonPropertyName("account_id")]
        public string? AccountId { get; set; }

        [JsonPropertyName("branch_id")]
        public int? BranchId { get; set; }

        [JsonPropertyName("source_id")]
        public string? SourceId { get; set; }
        [JsonPropertyName("last_updated")]
        public string? LastUpdated { get; set; }
    }
}