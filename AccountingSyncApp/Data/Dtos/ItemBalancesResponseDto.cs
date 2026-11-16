using System.Collections.Generic;

namespace AccountingSyncAPP.Data.Dtos
{
    // =========================================================================
    // الاستجابة الكاملة
    // =========================================================================

    /// <summary>
    /// موديل استجابة تقرير أرصدة الأصناف
    /// </summary>
    public class ItemBalancesResponseDto
    {
        public int     RowCount         { get; set; }
        public decimal GrandBalance     { get; set; }   // مجموع الرصيد الكلي
        public decimal GrandBalanceCost { get; set; }   // مجموع تكلفة الرصيد
        public decimal GrandSalesQty    { get; set; }   // مجموع كمية المبيعات
        public decimal GrandPurchaseQty { get; set; }   // مجموع كمية المشتريات
        public List<ItemBalancesRowDto> Rows { get; set; } = new List<ItemBalancesRowDto>();
    }

    // =========================================================================
    // صف واحد من التقرير
    // =========================================================================

    /// <summary>
    /// بيانات صف واحد في تقرير أرصدة الأصناف
    /// </summary>
    public class ItemBalancesRowDto
    {
        // --- 4 أعمدة ثابتة دائماً ---
        public string  ItemNo       { get; set; } = "";
        public string  ItemName     { get; set; } = "";
        public string  ItemUnit     { get; set; } = "";
        public decimal SellingPrice { get; set; }

        // --- أعمدة اختيارية وسطية (فارغة إن لم تُطلب) ---
        public string  BranchId     { get; set; } = "";
        public string  DepartmentId { get; set; } = "";
        public string  StoreNo      { get; set; } = "";
        public string  StoreName    { get; set; } = "";
        public string  ExpiryDate   { get; set; } = "";
        public string  BatchNumber  { get; set; } = "";

        // --- 16 عمود بيانات رئيسية ---
        public decimal BalanceQty       { get; set; }   // الرصيد
        public decimal UnitCost         { get; set; }   // تكلفة الوحدة
        public decimal BalanceCost      { get; set; }   // تكلفة الرصيد
        public decimal OpeningQty       { get; set; }   // ك افتتاحي
        public decimal PurchaseQty      { get; set; }   // ك مشتريات
        public decimal SalesQty         { get; set; }   // ك مبيعات
        public decimal OtherQty         { get; set; }   // ك اخرى (مردودات شراء + جرد)
        public decimal EndingQty        { get; set; }   // ك اخر المدة
        public decimal BalanceAtSelling { get; set; }   // الرصيد بسعر البيع
        public decimal IncomingQty      { get; set; }   // ك_الوارد
        public decimal OutgoingQty      { get; set; }   // ك_المنصرف
        public decimal BuyQty           { get; set; }   // ك شراء (= PurchaseQty)
        public decimal SellQty          { get; set; }   // ك البيع (= SalesQty)
        public decimal ReturnSellQty    { get; set; }   // ك مرتجع بيع
        public decimal BonusQty         { get; set; }   // بونص (مشتريات)
        public decimal SalesBonusQty    { get; set; }   // بونص مبيعات

        // --- عمود نهائي مشروط (فقط عند IncludeStore = true) ---
        public decimal ItemBalance { get; set; }        // رصيد الصنف
    }

    // =========================================================================
    // أنواع داخلية لقراءة نتائج SQL الخام — لا تُكشف للـ API
    // =========================================================================

    /// <summary>صف خام من استعلام حركات الفترة (Query A)</summary>
    internal class ItemBalancesMovementRaw
    {
        public string  ItemNo        { get; set; } = "";
        public string  ItemName      { get; set; } = "";
        public string  ItemUnit      { get; set; } = "";
        public decimal SellingPrice  { get; set; }

        // أعمدة اختيارية (فارغة إن لم تُطلب)
        public string  BranchId      { get; set; } = "";
        public string  DepartmentId  { get; set; } = "";
        public string  StoreNo       { get; set; } = "";
        public string  StoreName     { get; set; } = "";
        public string  ExpiryDate    { get; set; } = "";
        public string  BatchNumber   { get; set; } = "";

        public decimal UnitCost       { get; set; }   // من d.TRADITMUNTCOST
        public decimal SalesQty       { get; set; }   // 601,603,605,607,633,801,803
        public decimal PurchaseQty    { get; set; }   // 421,423
        public decimal PurchReturnQty { get; set; }   // 422,424
        public decimal SalesReturnQty { get; set; }   // 602,604,606,608,634,802,804
        public decimal InventoryQty   { get; set; }   // 507 (بإشارتها)
        public decimal IncomingQty    { get; set; }   // مشتريات + مردودات بيع
        public decimal OutgoingQty    { get; set; }   // مبيعات + مردودات مشتريات
        public decimal BonusQty       { get; set; }   // TRADQTYDIS على المشتريات
        public decimal SalesBonusQty  { get; set; }   // TRADQTYDIS على المبيعات
    }

    /// <summary>صف خام من استعلام الرصيد الافتتاحي (Query B)</summary>
    internal class ItemBalancesOpeningRaw
    {
        public string  ItemNo      { get; set; } = "";
        // مفاتيح التجميع الاختيارية (تُطابق Query A)
        public string  StoreNo     { get; set; } = "";
        public string  ExpiryDate  { get; set; } = "";
        public string  BatchNumber { get; set; } = "";
        public decimal OpeningQty  { get; set; }
        public decimal OpeningCost { get; set; }   // OpeningQty × UnitCost
    }
}
