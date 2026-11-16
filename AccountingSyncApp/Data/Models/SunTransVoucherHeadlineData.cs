namespace AccountingSyncAPP.Data.Dtos
{
    public class SunTransVoucherHeadlineData
    {
        public string TraId { get; set; }
        public string TraNo { get; set; }
        public string TraLstId { get; set; }
        public string TraYearId { get; set; }
        public string TraBraId { get; set; }
        public string TraSecId { get; set; }
        public string? OriginalTableName { get; set; }

        public string? TraTitle { get; set; }
        public string? TraDateId { get; set; }
        public string? TraTimeId { get; set; }
        public string? TraAccName { get; set; }   // cashBox name
        public string? TraCurId { get; set; }       // للبحث عن اسم العملة
        public string? TraPatName { get; set; }     // accountName (Beneficiary)
        public string? TraTot { get; set; }         // totalAmount
        public string? TraNetLet { get; set; }    // amountInWords
        public string? TrauId { get; set; }         // userId
        public string? TraRemarks { get; set; }   // description
        public string? TraCcId { get; set; }      // costCenterNo
        public string? TraAccId { get; set; }

        // --- ✨ الإضافات الضرورية لعمل دالة المزامنة الجديدة ✨ ---

        public string? TraDateTime { get; set; }   // لتحديد وقت التعديل (مهم جداً)
        public string? TraPatRefId { get; set; }   // رابط التزامن مع السب بيس (مهم جداً)

        // حقول إضافية يتم جلبها في الاستعلام لتجنب الأخطاء
        public string? TraDis { get; set; }
        public string? TraTax { get; set; }
        public string? TraNet { get; set; }
        public string? TraPatId { get; set; }
        public string? TraStoreId { get; set; }
    }
}