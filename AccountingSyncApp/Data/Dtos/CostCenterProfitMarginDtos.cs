using System;
using System.Collections.Generic;

namespace AccountingSyncAPP.Data.Dtos
{
    // =========================================================================
    // طلب تقرير هامش أرباح مراكز التكلفة
    // =========================================================================
    public class CostCenterProfitMarginRequestDto
    {
        public DateTime FromDate    { get; set; }
        public DateTime ToDate      { get; set; }
        public string   Year        { get; set; } = string.Empty;
        public int      BranchId    { get; set; }
        public int?     DepartmentId { get; set; }
    }

    // =========================================================================
    // صف واحد في النتيجة
    // =========================================================================
    public class CostCenterProfitMarginRowDto
    {
        public string  CostCenterId   { get; set; } = string.Empty;
        public string  CostCenterName { get; set; } = string.Empty;
        public decimal NetCogs        { get; set; }   // صافي المنصرف مخزنياً
        public decimal GrossSales     { get; set; }   // المبيعات الإجمالية
        public decimal Returns        { get; set; }   // المردودات
        public decimal NetSales       { get; set; }   // صافي المبيعات
        public decimal NetProfit      { get; set; }   // صافي الربح
        public decimal ProfitPct      { get; set; }   // نسبة الربح %
    }

    // =========================================================================
    // الاستجابة الكاملة مع المجاميع
    // =========================================================================
    public class CostCenterProfitMarginResponseDto
    {
        public List<CostCenterProfitMarginRowDto> Rows { get; set; } = new();
        public int     RowCount        { get; set; }
        public decimal GrandNetCogs    { get; set; }
        public decimal GrandGrossSales { get; set; }
        public decimal GrandReturns    { get; set; }
        public decimal GrandNetSales   { get; set; }
        public decimal GrandNetProfit  { get; set; }
    }
}
