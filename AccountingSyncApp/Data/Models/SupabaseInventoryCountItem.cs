using System;
using System.Text.Json.Serialization;

public class SupabaseInventoryCountItem
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [JsonPropertyName("countId")]
    public Guid CountId { get; set; }

    [JsonPropertyName("itemNo")]
    public string ItemNo { get; set; }

    [JsonPropertyName("actualQuantity")]
    public decimal ActualQuantity { get; set; }

    [JsonPropertyName("systemQuantity")]
    public decimal SystemQuantity { get; set; }
    
    [JsonPropertyName("notes")]
    public string? Notes { get; set; }
}
