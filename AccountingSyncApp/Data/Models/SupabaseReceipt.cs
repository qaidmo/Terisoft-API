using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace AccountingSyncAPP.Data.Models
{
    public class SupabaseReceipt
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("guid")]
        public string? Guid { get; set; }

        [JsonPropertyName("transactionNumber")]
        public string TransactionNumber { get; set; } = string.Empty;

        [JsonPropertyName("date")]
        public DateTime Date { get; set; }

        [JsonPropertyName("cashBox")]
        public string? CashBox { get; set; }

        [JsonPropertyName("boxAccountId")]
        public string? BoxAccountId { get; set; }

        [JsonPropertyName("currency")]
        public string Currency { get; set; } = string.Empty;

        [JsonPropertyName("currencyId")]
        public int CurrencyId { get; set; } = 1;

        [JsonPropertyName("currencyRate")]
        public decimal? CurrencyRate { get; set; }

        [JsonPropertyName("accountName")]
        public string? AccountName { get; set; }

        [JsonPropertyName("accountNo")]
        public string? AccountNo { get; set; }

        [JsonPropertyName("costCenter")]
        public string? CostCenter { get; set; }

        [JsonPropertyName("costCenterNo")]
        public int? CostCenterNo { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("totalAmount")]
        public decimal TotalAmount { get; set; }

        [JsonPropertyName("amountInWords")]
        public string? AmountInWords { get; set; }

        [JsonPropertyName("userId")]
        public int UserId { get; set; }

        [JsonPropertyName("userName")]
        public string? UserName { get; set; }

        [JsonPropertyName("invoiceTypeChar")]
        public string InvoiceTypeChar { get; set; } = string.Empty;

        [JsonPropertyName("invoiceType")]
        public string? InvoiceType { get; set; }

        [JsonPropertyName("collectorName")]
        public string? CollectorName { get; set; }

        [JsonPropertyName("collectorId")]
        public int? CollectorId { get; set; }

        [JsonPropertyName("agentId")]
        public int? AgentId { get; set; }

        [JsonPropertyName("agentName")]
        public string? AgentName { get; set; }

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

        [JsonPropertyName("items")]
        public List<SupabaseReceiptPaymentItem>? Items { get; set; }

        [JsonPropertyName("postings")]
        public List<SupabaseGeneralLedgerPosting>? Postings { get; set; }
    }

}