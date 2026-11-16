using System;
using System.Text.Json.Serialization;

namespace AccountingSyncAPP.Data.Models
{
    public class SupabaseSaleItem
    {
        // 1. تعديل المعرف ليتوافق مع SQLite (Integer)
        [JsonPropertyName("id")]
        public long Id { get; set; }

        // 2. تعديل معرف الربط بالفاتورة الأب
        [JsonPropertyName("saleId")]
        public long? SaleId { get; set; }

        [JsonPropertyName("itemNo")]
        public string? ItemNo { get; set; }

        // ✅ ممتاز: هذا الحقل موجود مسبقاً، سنستخدمه لتعويض الكاش
        [JsonPropertyName("itemName")]
        public string? ItemName { get; set; }

        [JsonPropertyName("quantity")]
        public decimal Quantity { get; set; }

        [JsonPropertyName("itemPrice")]
        public decimal ItemPrice { get; set; }

        [JsonPropertyName("total")]
        public decimal Total { get; set; }

        [JsonPropertyName("bonusQuantity")]
        public decimal? BonusQuantity { get; set; }

        [JsonPropertyName("itemDiscount")]
        public decimal? ItemDiscount { get; set; }

        // ⚠️ هام جداً: التطبيق يجب أن يرسل الوحدة هنا لأننا لن نجلبها من السيرفر
        [JsonPropertyName("itemUnit")]
        public string? ItemUnit { get; set; }

        [JsonPropertyName("packFactor")]
        public decimal? PackFactor { get; set; }

        [JsonPropertyName("expiryDate")]
        public string? ExpiryDate { get; set; }

        [JsonPropertyName("stockBalance")]
        public decimal? StockBalance { get; set; }

        [JsonPropertyName("batchNo")]
        public string? BatchNo { get; set; }

        [JsonPropertyName("costPrice")]
        public decimal? CostPrice { get; set; }

        [JsonPropertyName("diffQuantity")]
        public decimal? DiffQuantity { get; set; }

        [JsonPropertyName("deficitValue")]
        public decimal? DeficitValue { get; set; }

        [JsonPropertyName("surplusValue")]
        public decimal? SurplusValue { get; set; }

        [JsonPropertyName("itemCurrencyId")]
        public long? ItemCurrencyId { get; set; }

        [JsonPropertyName("itemCurrencyRate")]
        public decimal? ItemCurrencyRate { get; set; }

        [JsonPropertyName("pumpName")]
        public string? PumpName { get; set; }

        [JsonPropertyName("previousReading")]
        public decimal? PreviousReading { get; set; }

        [JsonPropertyName("currentReading")]
        public decimal? CurrentReading { get; set; }

        [JsonPropertyName("tradTraTax")]
        public decimal? TradTraTax { get; set; }
    }
}