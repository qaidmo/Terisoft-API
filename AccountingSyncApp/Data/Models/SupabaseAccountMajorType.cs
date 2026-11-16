using System.Text.Json.Serialization;

namespace AccountingSyncAPP.Data.Models
{
    public class SupabaseAccountMajorType
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("name_en")]
        public string? NameEn { get; set; }

        [JsonPropertyName("main_type_id")]
        public int MainTypeId { get; set; }

        [JsonPropertyName("source_id")]
        public string? SourceId { get; set; }
    }
}