using System.Text.Json.Serialization;

namespace AccountingSyncAPP.Data.Models
{
    public class SupabaseBoxCurrency
    {
        // لاحظ: لا نحتاج لـ ID هنا لأنه جدول ربط فقط

        [JsonPropertyName("box_no")]
        public int BoxNo { get; set; }

        [JsonPropertyName("currency_id")]
        public int CurrencyId { get; set; }
    }
}