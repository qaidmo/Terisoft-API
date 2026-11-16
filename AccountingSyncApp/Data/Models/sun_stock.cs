using System;
using System.Text.Json.Serialization;

namespace AccountingSyncAPP.Data.Models
{
    public class SupabaseStock
    {
        [JsonPropertyName("item_no")]
        public string? ItemNo { get; set; }

        // تم التعديل ليكون string ليقبل القيمة القادمة من الكنترولر مباشرة
        [JsonPropertyName("store_no")]
        public string? StoreNo { get; set; }

        // تم التعديل ليكون string ليقبل branchId القادم من حلقة الـ foreach
        [JsonPropertyName("branch_id")]
        public string? BranchId { get; set; }

        [JsonPropertyName("batch_no")]
        public string? BatchNo { get; set; }

        [JsonPropertyName("expiry_date")]
        public string? ExpiryDate { get; set; }

        [JsonPropertyName("item_name")]
        public string? ItemName { get; set; }

        [JsonPropertyName("quantity")]
        public double Quantity { get; set; }

        // تم تغيير الاسم ليتطابق مع طلب الكنترولر: s.LastTransactionStr
        [JsonPropertyName("last_updated_str")]
        public string? LastTransactionStr { get; set; }

        [JsonPropertyName("updated_at")]
        public DateTime UpdatedAt { get; set; }

        [JsonPropertyName("cost_price")]
        public double CostPrice { get; set; }
    }
}