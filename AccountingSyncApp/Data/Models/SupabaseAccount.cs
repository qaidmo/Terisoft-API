using System;
using System.Text.Json.Serialization;

namespace AccountingSyncAPP.Data.Models
{
    public class SupabaseAccount
    {
        // تم تغيير النوع من Guid إلى string ليتوافق مع رقم الحساب الأصلي
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("account_number")]
        public string AccountNumber { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("name_en")]
        public string? NameEn { get; set; }

        // تم تغيير النوع إلى string? للسماح بربط حساب الأب نصياً
        [JsonPropertyName("parent_id")]
        public string? ParentId { get; set; }

        // تم تغيير bool إلى int ليرسل 0 أو 1 توافقاً مع SQLite
        [JsonPropertyName("is_transactional")]
        public int IsTransactional { get; set; }

        [JsonPropertyName("is_active")]
        public int IsActive { get; set; }

        [JsonPropertyName("account_type_id")]
        public int? AccountTypeId { get; set; }

        [JsonPropertyName("classification_id")]
        public int? ClassificationId { get; set; }

        [JsonPropertyName("default_currency_id")]
        public int? DefaultCurrencyId { get; set; }

        [JsonPropertyName("nature")]
        public string? Nature { get; set; } // مدين / دائن

        [JsonPropertyName("source_id")]
        public string SourceId { get; set; }
    }
}