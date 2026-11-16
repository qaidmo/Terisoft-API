using System.Collections.Generic;

namespace AccountingSyncAPP.Data.Dtos
{
    /// <summary>
    /// موديل استجابة تقرير هامش الربح
    /// </summary>
    public class ProfitMarginResponseDto
    {
        public int    RowCount        { get; set; }
        public decimal GrandSalesQty  { get; set; }
        public decimal GrandSalesValue { get; set; }
        public decimal GrandSalesCost  { get; set; }
        public decimal GrandMargin     { get; set; }
        public decimal GrandBalanceQty { get; set; }
        public decimal GrandBalanceCost{ get; set; }
        public List<ProfitMarginRowDto> Rows { get; set; } = new List<ProfitMarginRowDto>();
    }

    /// <summary>
    /// بيانات صف واحد في تقرير هامش الربح
    /// PrimaryKey / PrimaryName تتغير بناءً على GroupBy (صنف / مجموعة / مستخدم)
    /// </summary>
    public class ProfitMarginRowDto
    {
        // --- عمود التجميع الأول (رقم الصنف / رقم المجموعة / رقم المستخدم) ---
        public string PrimaryKey  { get; set; } = "";

        // --- عمود التجميع الثاني (اسم الصنف / اسم المجموعة / اسم المستخدم) ---
        public string PrimaryName { get; set; } = "";

        // --- يُعبأ فقط عند GroupBy = "item" ---
        public string ItemUnit { get; set; } = "";

        // --- أعمدة اختيارية (فارغة إن لم تُطلب) ---
        public string BranchId      { get; set; } = "";
        public string DepartmentId  { get; set; } = "";
        public string BatchNumber   { get; set; } = "";
        public string CostCenterName{ get; set; } = "";

        // --- مبيعات ---
        public decimal SalesQty   { get; set; }   // كمية المبيعات (مردودات مطروحة)
        public decimal SalesValue  { get; set; }   // قيمة المبيعات المحلية
        public decimal SalesCost   { get; set; }   // تكلفة المبيعات

        // --- هامش الربح (يُحسب في C#) ---
        public decimal Margin      { get; set; }   // SalesValue - SalesCost
        public decimal ProfitPct   { get; set; }   // Margin / SalesCost * 100

        // --- رصيد المخزون ---
        public decimal BalanceQty  { get; set; }   // الرصيد الفعلي
        public decimal BalanceCost { get; set; }   // تكلفة الرصيد

        // --- نسبة مبيعات الرصيد (يُحسب في C#) ---
        // Formula: SalesQty / BalanceQty * 100   (صفر إذا BalanceQty = 0)
        public decimal BalanceSalesPct { get; set; }
    }

    // ---------------------------------------------------------------------------
    // DTOs الداخلية لقراءة نتائج SQL الخام (لا تُكشف للـ API)
    // ---------------------------------------------------------------------------

    /// <summary>صف خام من استعلام المبيعات المجمّعة</summary>
    internal class ProfitMarginSalesRaw
    {
        public string  PrimaryKey   { get; set; } = "";
        public string  PrimaryName  { get; set; } = "";
        public string  ItemUnit     { get; set; } = "";
        public string  BranchId     { get; set; } = "";
        public string  DepartmentId { get; set; } = "";
        public string  BatchNumber  { get; set; } = "";
        public string  CostCenterName { get; set; } = "";
        public decimal SalesQty     { get; set; }
        public decimal SalesValue   { get; set; }
        public decimal SalesCost    { get; set; }
    }

    /// <summary>صف خام من استعلام رصيد المخزون المجمّع</summary>
    internal class ProfitMarginBalanceRaw
    {
        public string  PrimaryKey   { get; set; } = "";
        public decimal BalanceQty   { get; set; }
        public decimal BalanceCost  { get; set; }
    }
}
