using System;
using System.Text.Json.Serialization;

namespace AccountingSyncAPP.Data.Models
{
    public class SupabaseFinancialYear
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("year_name")]
        public string? YearName { get; set; }

        [JsonPropertyName("start_date")]
        public DateTime StartDate { get; set; }

        [JsonPropertyName("end_date")]
        public DateTime EndDate { get; set; }

        [JsonPropertyName("source_id")]
        public string? SourceId { get; set; }
    }
}