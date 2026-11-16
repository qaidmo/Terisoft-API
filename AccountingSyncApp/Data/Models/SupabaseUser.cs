using System;
using System.Text.Json.Serialization; // 👈 المكتبة المعتمدة

namespace AccountingSyncAPP.Data.Models
{
    public class SupabaseUser
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("user_no")]
        public int UserNo { get; set; }

        [JsonPropertyName("user_name")]
        public string? UserName { get; set; }

        [JsonPropertyName("user_type")]
        public string? UserType { get; set; } // admin / user

        [JsonPropertyName("user_group")]
        public string? UserGroup { get; set; }

        [JsonPropertyName("user_branch")]
        public int UserBranch { get; set; }

        [JsonPropertyName("user_department")]
        public int UserDepartment { get; set; }

        // 🧠 التعديل: تحويله لنص ليتوافق مع UStoreId في الكنترولر
        [JsonPropertyName("store_id")]
        public string? StoreId { get; set; }

        [JsonPropertyName("box_account_id")]
        public string? BoxAccountId { get; set; }
        [JsonPropertyName("receipt_box_no")]
        public int? ReceiptBoxNo { get; set; }

        [JsonPropertyName("payment_box_no")]
        public int? PaymentBoxNo { get; set; }

        [JsonPropertyName("password_hash")]
        public string? PasswordHash { get; set; } 

        [JsonPropertyName("source_id")]
        public string? SourceId { get; set; }

        [JsonPropertyName("modified_at")]
        public DateTime? ModifiedAt { get; set; }

        [JsonPropertyName("user_status")]
        public string? UserStatus { get; set; }
    }
}