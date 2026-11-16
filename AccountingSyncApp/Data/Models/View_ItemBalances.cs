using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AccountingSyncAPP.Data.Models
{
    public class SunItemBalance
    {
        [Column("ItemNo")]
        public string? ItemNo { get; set; }

        [Column("ItemName")]
        public string? ItemName { get; set; }

        [Column("StoreNo")]
        public string? StoreNo { get; set; }

        [Column("ExpiryDate")]
        public string? ExpiryDate { get; set; }

        [Column("Quantity")]
        public double Quantity { get; set; }

        [Column("LastTransactionStr")]
        public string? LastTransactionStr { get; set; }

        [Column("BatchNo")]
        public string? BatchNo { get; set; }

        [Column("BranchId")]
        public string? BranchId { get; set; }

        [Column("CostPrice")]
        public double CostPrice { get; set; }
    }
}