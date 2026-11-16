using System.Text.Json.Serialization;

namespace AccountingSyncAPP.Data.Models
{
    public class SupabaseItemUnit
    {
        [JsonPropertyName("unit_id")]
        public string? UnitId { get; set; }

        [JsonPropertyName("item_no")]
        public string? ItemNo { get; set; }

        [JsonPropertyName("unit_name")]
        public string? UnitName { get; set; }

        [JsonPropertyName("unit_index")]
        public int UnitIndex { get; set; }

        [JsonPropertyName("conversion_factor")]
        public double ConversionFactor { get; set; }

        [JsonPropertyName("sale_price")]
        public double SalePrice { get; set; }

        [JsonPropertyName("barcode")]
        public string? Barcode { get; set; }

        [JsonPropertyName("is_active")]
        public bool IsActive { get; set; }

        [JsonPropertyName("last_updated")]
        public string? LastUpdated { get; set; }
    }
}