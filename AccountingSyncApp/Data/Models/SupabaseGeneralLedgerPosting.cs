using System;
using System.Text.Json.Serialization;

namespace AccountingSyncAPP.Data.Models
{
    public class SupabaseGeneralLedgerPosting
    {
        // 1. تعديل المعرف ليتوافق مع SQLite
        [JsonPropertyName("id")]
        public long Id { get; set; }

        // --- بيانات الحساب (تم تعزيزها لتجنب الكاش) ---
        [JsonPropertyName("accountNo")]
        public string? AccountNo { get; set; }

        // 🆕 جديد: اسم الحساب (ضروري جداً للقيد)
        [JsonPropertyName("accountName")]
        public string? AccountName { get; set; }

        // 🆕 جديد: معرف الحساب (GUID) الخاص بـ SQL Server إذا كان متوفراً في التطبيق
        // يفضل إرساله كنص لتسهيل التعامل
        [JsonPropertyName("accountId")]
        public string? AccountId { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("originalAmount")]
        public decimal OriginalAmount { get; set; }

        [JsonPropertyName("currencyId")]
        public int CurrencyId { get; set; }

        [JsonPropertyName("exchangeRate")]
        public decimal ExchangeRate { get; set; }

        [JsonPropertyName("debit")]
        public decimal? Debit { get; set; }

        [JsonPropertyName("credit")]
        public decimal? Credit { get; set; }

        [JsonPropertyName("transactionDate")]
        public DateTime TransactionDate { get; set; }

        // 2. تعديل معرف الربط بالفاتورة الأب
        [JsonPropertyName("transactionId")]
        public long? TransactionId { get; set; }

        [JsonPropertyName("transactionType")]
        public string? TransactionType { get; set; }

        [JsonPropertyName("transactionRef")]
        public string? TransactionRef { get; set; }

        // --- مراكز التكلفة ---
        [JsonPropertyName("costCenterNo")]
        public int? CostCenterNo { get; set; }

        // 🆕 جديد: اسم مركز التكلفة
        [JsonPropertyName("costCenterName")]
        public string? CostCenterName { get; set; }

        [JsonPropertyName("beneficiaryName")]
        public string? BeneficiaryName { get; set; }
        [JsonPropertyName("controlAccountId")]
        public string? ControlAccountId { get; set; }
    }
}