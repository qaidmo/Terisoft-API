using System.Collections.Generic;

namespace AccountingSyncAPP.Data.Models
{
    public class RestaurantInvoiceDto
    {
        public string? InvoiceNumber { get; set; }
        public string? OrderNumber { get; set; }

        public string? CustomerName { get; set; }
        public string TraLstId { get; set; } = "";
        public string TraTitle { get; set; } = "";

        public string? BoxId { get; set; }
        public string? BoxName { get; set; }

        public string? BoxNo { get; set; }
        public string? BoxParentName { get; set; }

        public string? StoreId { get; set; }
        public string? StoreName { get; set; }

        public string? CurrencyId { get; set; }
        public decimal CurrencyValue { get; set; }

        public decimal Discount { get; set; }
        public decimal Tax { get; set; }
        public decimal Total { get; set; }
        public decimal NetTotal { get; set; }

        public string? NetTotalInWords { get; set; }

        public string? UserId { get; set; }
        public string? UserName { get; set; }
        public string? BranchId { get; set; }
        public string? SectionId { get; set; }
        public string? YearId { get; set; }
        public string? TransactionDate { get; set; }

        public List<RestaurantInvoiceDetailDto> Details { get; set; } = new List<RestaurantInvoiceDetailDto>();
        public string? OrderTypeId { get; set; }

        public bool IsNetworkPayment { get; set; }
        public decimal PaidAmount { get; set; }
        
        public string? CreatedAt { get; set; }
    }

    public class RestaurantInvoiceDetailDto
    {
        public string? ItemId { get; set; }
        public string? ItemName { get; set; }
        public string? Unit { get; set; }
        public decimal Quantity { get; set; }
        public decimal Price { get; set; }
        public decimal? Total { get; set; }
        public string? Notes { get; set; }
        public decimal Discount { get; set; }
        public decimal Tax { get; set; }
        public decimal? PackFactor { get; set; }
    }
}
