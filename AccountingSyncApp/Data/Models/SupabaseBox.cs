using System;
using System.Text.Json.Serialization;

namespace AccountingSyncAPP.Data.Models
{
    public class SupabaseBox
    {
        [JsonPropertyName("box_no")]
        public int BoxNo { get; set; }

        [JsonPropertyName("box_name")]
        public string BoxName { get; set; }

        // 👇 التغيير هنا: حولناها إلى string
        [JsonPropertyName("account_id")]
        public string? AccountId { get; set; }

        // 👇 التغيير هنا: حولناها إلى string
        [JsonPropertyName("control_account_id")]
        public string? ControlAccountId { get; set; }

        [JsonPropertyName("source_id")]
        public string? SourceId { get; set; }
    }
}