using System.Collections.Generic;

namespace AccountingSyncAPP.Data.Dtos
{
    // =========================================================================
    // نتيجة استعلام SQL الخام — يتطابق 1:1 مع أعمدة SELECT
    // =========================================================================
    public class SupplierMovementRawData
    {
        public string AccountNumber    { get; set; }
        public string AccountName      { get; set; }
        public string SupplierName     { get; set; }
        public string DepartmentId     { get; set; }
        public string BranchId         { get; set; }
        public decimal OpeningBalance  { get; set; }
        public decimal TotalPurchases  { get; set; }
        public decimal TotalReturns    { get; set; }
        public decimal TotalPayments   { get; set; }
        public decimal TotalAdjustments { get; set; }
        public string? LastPaymentDate  { get; set; }
    }

    // =========================================================================
    // موديل السطر المُعالَج — يُرسَل إلى Flutter
    // =========================================================================
    public class SupplierMovementItemDto
    {
        public string AccountNumber      { get; set; }   // رقم الحساب
        public string AccountName        { get; set; }   // اسم الحساب
        public string SupplierName       { get; set; }   // اسم المورد
        public string DepartmentId       { get; set; }   // القسم
        public string BranchId           { get; set; }   // الفرع

        public decimal OpeningBalance    { get; set; }   // الرصيد الافتتاحي
        public decimal TotalPurchases    { get; set; }   // مشتريات (421, 423)
        public decimal TotalReturns      { get; set; }   // مردودات (422, 424)
        public decimal NetPurchases      { get; set; }   // صافي المشتريات = مشتريات - مردودات
        public decimal TotalPayments     { get; set; }   // السداد (202)
        public decimal TotalAdjustments  { get; set; }   // تسويات (201 عكسي)
        public decimal Balance           { get; set; }   // الرصيد = افتتاحي + صافي - سداد - تسويات

        public decimal InventoryValue    { get; set; }   // قيمة المخزون (SUM TRADQTY × TRADITMUNTCOST)
        public decimal Difference        { get; set; }   // الفارق = رصيد - مخزون (Summary) | فاتورة - مخزون (Detailed)

        public string LastPaymentDate    { get; set; }   // تاريخ آخر سداد

        // حقول Detailed Mode فقط — فارغة في Summary Mode
        public string InvoiceDate        { get; set; } = "";   // تاريخ الفاتورة
        public string InvoiceNumber      { get; set; } = "";   // رقم الفاتورة
    }

    // =========================================================================
    // نتيجة SQL الخام لـ Detailed Mode — يتطابق 1:1 مع SELECT في GetDetailedModeAsync
    // =========================================================================
    public class SupplierMovementDetailRawData
    {
        public string  AccountNumber          { get; set; }
        public string  AccountName            { get; set; }
        public string  SupplierName           { get; set; }
        public string  DepartmentId           { get; set; }
        public string  BranchId               { get; set; }
        public string? InvoiceDate            { get; set; }
        public string? InvoiceNumber          { get; set; }
        public decimal OpeningBalance         { get; set; }   // للمطابقة مع استعلام الرصيد الافتتاحي
        public decimal TotalPurchases         { get; set; }   // 421, 423
        public decimal TotalReturns           { get; set; }   // 422, 424
        public decimal TotalPayments          { get; set; }   // 202
        public decimal TotalAdjustments       { get; set; }   // 201
        public decimal InvoiceInventoryValue  { get; set; }   // SUM(TRADQTY × TRADITMUNTCOST)
    }

    // =========================================================================
    // الاستجابة الكاملة — تشمل القائمة والمجاميع
    // =========================================================================
    public class SupplierMovementResponseDto
    {
        public List<SupplierMovementItemDto> Data { get; set; } = new List<SupplierMovementItemDto>();

        // مجاميع عامة لعرضها في تذييل الجدول
        public decimal GrandOpeningBalance  { get; set; }
        public decimal GrandTotalPurchases  { get; set; }
        public decimal GrandTotalReturns    { get; set; }
        public decimal GrandNetPurchases    { get; set; }
        public decimal GrandTotalPayments   { get; set; }
        public decimal GrandTotalAdjustments { get; set; }
        public decimal GrandBalance         { get; set; }
    }
}
