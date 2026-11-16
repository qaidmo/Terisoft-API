using System.Text.Json.Serialization;

namespace AccountingSyncAPP.Data.Models
{
    public class SupabasePermission
    {
        [JsonPropertyName("user_no")]
        public int UserNo { get; set; }

        [JsonPropertyName("screen_name")]
        public string ScreenName { get; set; } = string.Empty;

        [JsonPropertyName("can_access")]
        public bool CanAccess { get; set; }

        [JsonPropertyName("can_add")]
        public bool CanAdd { get; set; }

        [JsonPropertyName("can_edit")]
        public bool CanEdit { get; set; }

        [JsonPropertyName("can_search")]
        public bool CanSearch { get; set; }

        [JsonPropertyName("can_print")]
        public bool CanPrint { get; set; }

        [JsonPropertyName("can_change_price")]
        public bool CanChangePrice { get; set; }

        [JsonPropertyName("created_at")]
        public string? CreatedAt { get; set; }

        [JsonPropertyName("updated_at")]
        public string? UpdatedAt { get; set; }
    }
}