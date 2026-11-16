using System.Collections.Generic;

namespace AccountingSyncAPP.Data.Dtos
{
    /// <summary>
    /// موديل استجابة تقرير بطاقة الصنف
    /// </summary>
    public class ItemCardResponseDto
    {
        public int     RowCount          { get; set; }
        public decimal TotalQuantityIn   { get; set; }
        public decimal TotalQuantityOut  { get; set; }
        public decimal FinalBalance      { get; set; }
        public List<ItemCardRowDto> Rows { get; set; } = new List<ItemCardRowDto>();
    }

    /// <summary>
    /// بيانات صف واحد في تقرير بطاقة الصنف (23 عمود)
    /// جميع الأسماء هنا تطابق أسماء الأعمدة في SELECT SQL
    /// </summary>
    public class ItemCardRowDto
    {
        public string  ItemNo            { get; set; }  // رقم الصنف
        public string  BranchName        { get; set; }  // الفرع (h.TRABRAID)
        public string  CostCenterName    { get; set; }  // مركز التكلفة (h.TRACCNAME)
        public string  AccountName       { get; set; }  // اسم الحساب (h.TRAACCNAME)
        public string  StoreName         { get; set; }  // المخزن (h.TRASTORENAME)
        public string  ReferenceNumber   { get; set; }  // مرجع (h.TRAREFR)
        public string  DateNo            { get; set; }  // رقم_التاريخ (h.TRADATEID = yyyyMMdd)
        public decimal UnitPrice         { get; set; }  // السعر (d.TRADUNTVALUE)
        public decimal TotalAmount       { get; set; }  // المبلغ (d.TRADAMOUNT)
        public decimal TotalCost         { get; set; }  // التكاليف (qty * unitcost)
        public string  PackFactor        { get; set; }  // علبة/كرتون (d.TRADUNITSIZE)
        public string  ExpiryDate        { get; set; }  // تاريخ انتهاء (d.TRADEXPIRE)
        public string  CustomerName      { get; set; }  // الاسم - عميل (h.TRAPATNAME)
        public string  TransactionNumber { get; set; }  // رقم الحركة (h.TRANO)
        public string  Description       { get; set; }  // البيان (h.TRATITLE)
        public string  Date              { get; set; }  // التاريخ (h.TRADATE)
        public string  ItemUnit          { get; set; }  // الوحدة (d.TRADITMUNIT)
        public decimal CostPrice         { get; set; }  // التكلفة (d.TRADITMUNTCOST)
        public string  BatchNumber       { get; set; }  // رقم القطعة (d.TRADREFID)
        public decimal QuantityOut       { get; set; }  // صادر (CASE OUT types)
        public decimal QuantityIn        { get; set; }  // وارد (CASE IN types)
        public decimal Balance           { get; set; }  // الرصيد - running SUM() OVER
    }
}
