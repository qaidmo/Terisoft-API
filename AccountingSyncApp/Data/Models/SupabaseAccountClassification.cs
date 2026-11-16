using System;
using System.Text.Json.Serialization;

namespace AccountingSyncAPP.Data.Models
{
    public class SupabaseAccountClassification
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("name_en")]
        public string? NameEn { get; set; }

        [JsonPropertyName("major_type_id")]
        public int MajorTypeId { get; set; }

        // تم التغيير من Guid? إلى string? ليتوافق مع أرقام الحسابات النصية
        [JsonPropertyName("default_parent_account_id")]
        public string? DefaultParentAccountId { get; set; }

        [JsonPropertyName("source_id")]
        public string? SourceId { get; set; }
    }
}