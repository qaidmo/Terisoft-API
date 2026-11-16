using System;
using System.Text.Json.Serialization; // 👈 هذه المكتبة ضرورية جداً

namespace AccountingSyncAPP.Data.Models
{
    // ملاحظة: الاسم SupabaseItem لم يعد دقيقاً لكن لا بأس من تركه لعدم تكسير الكود
    public class SupabaseItem
    {
        [JsonPropertyName("item_no")] 
        public string? ItemNo { get; set; }

        [JsonPropertyName("item_name")]
        public string? ItemName { get; set; }

        [JsonPropertyName("item_barcode")]
        public string? ItemBarcode { get; set; }

        [JsonPropertyName("item_unit")]
        public string? ItemUnit { get; set; }

        [JsonPropertyName("item_price")]
        public double ItemPrice { get; set; }
        [JsonPropertyName("average_cost")]
        public string? AverageCost { get; set; }

        [JsonPropertyName("section_no")]
        public string? SectionNo { get; set; }

        [JsonPropertyName("item_group_no")]
        public int? ItemGroupNo { get; set; }

        [JsonPropertyName("currency_no")]
        public int CurrencyNo { get; set; }

        [JsonPropertyName("inventory_account_id")]
        public string? InventoryAccountId { get; set; }

        [JsonPropertyName("cogs_account_id")]
        public string? CogsAccountId { get; set; }

        [JsonPropertyName("sales_revenue_account_id")]
        public string? SalesRevenueAccountId { get; set; }

        [JsonPropertyName("sales_discount_account_id")]
        public string? SalesDiscountAccountId { get; set; }

        [JsonPropertyName("tax_rate")]
        public decimal? TaxRate { get; set; }

        [JsonPropertyName("cost_center_no")]
        public int? CostCenterNo { get; set; }

        [JsonPropertyName("discount_rate")]
        public decimal? DiscountRate { get; set; }

        [JsonPropertyName("is_taxable")]
        public bool IsTaxable { get; set; }

        [JsonPropertyName("is_discountable")]
        public bool IsDiscountable { get; set; }

        [JsonPropertyName("is_cash_reserved")]
        public bool IsCashReserved { get; set; }
        [JsonPropertyName("last_updated")]
        public string? LastUpdated { get; set; }
    }
}