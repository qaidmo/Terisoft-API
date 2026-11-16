using System;
using System.Text.Json.Serialization;

namespace AccountingSyncAPP.Data.Models
{
    public class SupabaseStore
    {
        [JsonPropertyName("no_store")]
        public int NoStore { get; set; }

        [JsonPropertyName("name_store")]
        public string? NameStore { get; set; }

        [JsonPropertyName("branch_id")]
        public int BranchId { get; set; }

        // 👇 التغيير هنا: حولناها إلى string
        [JsonPropertyName("inventory_account_id")]
        public string? InventoryAccountId { get; set; }

        [JsonPropertyName("source_id")]
        public string? SourceId { get; set; }
    }
}