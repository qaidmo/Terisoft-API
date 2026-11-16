using System.Text.Json.Serialization;

namespace AccountingSyncAPP.Data.Models
{
    public class SupabaseBranch
    {
        [JsonPropertyName("branch_id")]
        public int BranchId { get; set; }

        [JsonPropertyName("branch_name")]
        public string? BranchName { get; set; }

        [JsonPropertyName("source_id")]
        public string? SourceId { get; set; }
    }
}