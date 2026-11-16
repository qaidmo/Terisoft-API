using System.Text.Json.Serialization;

namespace AccountingSyncAPP.Data.Models
{
    public class SupabaseCostCenter
    {
        [JsonPropertyName("center_no")]
        public int CenterNo { get; set; }

        [JsonPropertyName("center_name")]
        public string? CenterName { get; set; }

        [JsonPropertyName("source_id")]
        public string? SourceId { get; set; }

        [JsonPropertyName("last_updated")]
        public string? LastUpdated { get; set; }
    }
}