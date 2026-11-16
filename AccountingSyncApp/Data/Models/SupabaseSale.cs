using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace AccountingSyncAPP.Data.Models
{
    public class SupabaseSale
    {
        // 1. المعرف القادم من SQLite (رقمي)
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("guid")]
        public string? Guid { get; set; }

        [JsonPropertyName("transactionNumber")]
        public string TransactionNumber { get; set; }

        [JsonPropertyName("date")]
        public DateTime Date { get; set; }
        [JsonPropertyName("currencyId")]
        public int? CurrencyId { get; set; }

        [JsonPropertyName("currency")]
        public string Currency { get; set; }
        
        [JsonPropertyName("currencyRate")]
        public decimal? CurrencyRate { get; set; }
        [JsonPropertyName("currencySymbol")]
        public string? CurrencySymbol { get; set; }

        [JsonPropertyName("customerName")]
        public string? CustomerName { get; set; }

        // --- البيانات المالية ---
        [JsonPropertyName("total")]
        public decimal Total { get; set; }

        [JsonPropertyName("discount")]
        public decimal? Discount { get; set; }

        [JsonPropertyName("tax")]
        public decimal? Tax { get; set; }

        [JsonPropertyName("netTotal")]
        public decimal NetTotal { get; set; }

        [JsonPropertyName("createdAt")]
        public DateTimeOffset CreatedAt { get; set; }

        [JsonPropertyName("userId")]
        public int UserId { get; set; }

        [JsonPropertyName("userName")]
        public string? UserName { get; set; }

        [JsonPropertyName("invoiceType")]
        public string? InvoiceType { get; set; }

        [JsonPropertyName("invoiceTypeChar")]
        public string InvoiceTypeChar { get; set; }

        [JsonPropertyName("accountNo")]
        public string? AccountNo { get; set; }
        [JsonPropertyName("accountId")]
        public string? AccountId { get; set; }

        // --- الصناديق ---
        [JsonPropertyName("boxNo")]
        public int? BoxNo { get; set; }

        [JsonPropertyName("boxName")] // مفتاح JSON المقترح
        public string? BoxName { get; set; }

        [JsonPropertyName("boxAccountId")]
        public string? BoxAccountId { get; set; }

        // --- المخازن ---
        [JsonPropertyName("storeNo")]
        public int? StoreNo { get; set; }

        [JsonPropertyName("storeName")] // مفتاح JSON المقترح
        public string? StoreName { get; set; }

        [JsonPropertyName("isSynced")]
        public bool IsSynced { get; set; }

        [JsonPropertyName("sunYear")]
        public int SunYear { get; set; }

        [JsonPropertyName("branchId")]
        public int BranchId { get; set; }

        [JsonPropertyName("departmentId")]
        public int? DepartmentId { get; set; }

        [JsonPropertyName("sourceLastUpdatedAt")]
        public DateTime? SourceLastUpdatedAt { get; set; }

        [JsonPropertyName("systemTotalValue")]
        public decimal? SystemTotalValue { get; set; }

        // --- المحصل والمندوب ---
        [JsonPropertyName("collectorId")]
        public int? CollectorId { get; set; }

        [JsonPropertyName("collectorName")] // مفتاح JSON المقترح
        public string? CollectorName { get; set; }

        [JsonPropertyName("agentId")]
        public int? AgentId { get; set; }

        [JsonPropertyName("agentName")] // مفتاح JSON المقترح
        public string? AgentName { get; set; }

        [JsonPropertyName("traSrv")]
        public decimal? TraSrv { get; set; }

        // --- العلاقات ---
        [JsonPropertyName("items")]
        public List<SupabaseSaleItem>? Items { get; set; }

        [JsonPropertyName("postings")]
        public List<SupabaseGeneralLedgerPosting>? Postings { get; set; }
    }
}