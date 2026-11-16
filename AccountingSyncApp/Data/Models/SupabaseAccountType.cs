using System.Text.Json.Serialization;

namespace AccountingSyncAPP.Data.Models
{
    public class SupabaseAccountType
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("name_en")]
        public string? NameEn { get; set; }

        [JsonPropertyName("source_id")]
        public string? SourceId { get; set; }
    }
}