using System.Text.Json.Serialization;

namespace AccountingSyncAPP.Data.Models
{
    public class SupabaseItemGroup
    {
        [JsonPropertyName("group_no")]
        public int GroupNo { get; set; }

        [JsonPropertyName("group_name")]
        public string? GroupName { get; set; }

        [JsonPropertyName("main_group_no")]
        public int? MainGroupNo { get; set; }

        [JsonPropertyName("source_id")]
        public string? SourceId { get; set; }
        [JsonPropertyName("last_updated")]
        public string? LastUpdated { get; set; }
    }
}