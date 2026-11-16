using System.Text.Json.Serialization;

namespace AccountingSyncAPP.Data.Models
{
    public class SupabaseMainGroup
    {
        [JsonPropertyName("main_group_no")]
        public int MainGroupNo { get; set; }

        [JsonPropertyName("main_group_name")]
        public string? MainGroupName { get; set; }

        [JsonPropertyName("source_id")]
        public string? SourceId { get; set; }
        [JsonPropertyName("last_updated")]
        public string? LastUpdated { get; set; }
    }
}