using System;
using System.Text.Json.Serialization;

public class SupabaseInventoryCount
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [JsonPropertyName("transactionNumber")]
    public string TransactionNumber { get; set; }

    [JsonPropertyName("date")]
    public DateTime Date { get; set; }

    [JsonPropertyName("storeNo")]
    public int StoreNo { get; set; }

    [JsonPropertyName("notes")]
    public string? Notes { get; set; }

    [JsonPropertyName("isSynced")]
    public bool IsSynced { get; set; }

    [JsonPropertyName("userId")]
    public int? UserId { get; set; }

    [JsonPropertyName("branchId")]
    public int? BranchId { get; set; }
    
    [JsonPropertyName("createdAt")]
    public DateTimeOffset CreatedAt { get; set; }
}
