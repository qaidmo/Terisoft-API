using System.Text.Json.Serialization;

namespace AccountingSyncAPP.Data.Models
{
    public class SupabaseDepartment
    {
        [JsonPropertyName("department_id")]
        public int DepartmentId { get; set; }

        [JsonPropertyName("department_name")]
        public string? DepartmentName { get; set; }

        [JsonPropertyName("source_id")]
        public string? SourceId { get; set; }
    }
}