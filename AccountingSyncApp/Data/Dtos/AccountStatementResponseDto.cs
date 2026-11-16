namespace AccountingSyncAPP.Data.Dtos
{
    public class StatementTransactionDto
    {
        public string TraId { get; set; }        // معرف الحركة (للمراجعة)
        public string TraNo { get; set; }        // رقم المستند الظاهر
        public string Date { get; set; }         // التاريخ (dd/MM/yyyy)
        public string Type { get; set; }         // نوع الحركة (فاتورة مبيعات، سند قبض...)
        public string Description { get; set; }  // البيان / الشرح
        public decimal Debit { get; set; }       // مدين (محول لرقم عشري)
        public decimal Credit { get; set; }      // دائن (محول لرقم عشري)
        public string Beneficiary { get; set; }

        public decimal Balance { get; set; }     // الرصيد التراكمي المحسوب من السيرفر
    }
}