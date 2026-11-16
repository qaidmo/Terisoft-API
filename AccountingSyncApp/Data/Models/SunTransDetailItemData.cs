namespace AccountingSyncAPP.Data.Dtos 
{
      public class SunTransDetailItemData
    {
        public string TraId { get; set; }          // للربط مع Headline

        public string TradItmId { get; set; }      // itemNo
        public string TradQty { get; set; }        // quantity
        public string TradQtyDis { get; set; }     // bonusQuantity
        public string TradUntValue { get; set; }   // itemPrice
        public string TradUntDis { get; set; }     // itemDiscount
        public string TradNetAmount { get; set; } // total
        public string TradItmUnit { get; set; }    // الوحدة (item_unit) - مثال: "حبة"، "كرتون"

        public string TradUnitSize { get; set; }   // العبوة (pack_factor) - مثال: "1", "24"

        public string TradExpire { get; set; }     // تاريخ الصلاحية نصي (expiry_date) - مثال: "20/02/2026"

        public string TradExpireId { get; set; }

        // --- Added for Reverse Sync ---
        public string? TradQtyOth { get; set; }    // StockBalance
        public string? TradRefId { get; set; }     // BatchNo
        public string? TradItmUntCost { get; set; } // CostPrice
        public string? TradX { get; set; }         // DiffQuantity (or Value?)
        public string? TradY { get; set; }         // DiffVal (or Qty?)
        public string? TRADOTH { get; set; }       // Deficit/Surplus
    }
}