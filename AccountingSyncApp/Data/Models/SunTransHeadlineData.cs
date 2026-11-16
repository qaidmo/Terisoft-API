namespace AccountingSyncAPP.Data.Dtos
{
    public class SunTransHeadlineData
    {
        // جعلنا كل شيء Nullable (?) لتجنب أخطاء Data is Null
        public string? TraId { get; set; }           // المفتاح الأساسي
        public string? TraNo { get; set; }           // رقم الفاتورة
        public string? TraLstId { get; set; }        // نوع الفاتورة
        public string? TraYearId { get; set; }       // السنة
        public string? TraBraId { get; set; }        // الفرع
        public string? TraSecId { get; set; }        // القسم
        public string? OriginalTableName { get; set; } // اسم الجدول

        // --- الأعمدة الأساسية للمزامنة ---
        public string? TraDateTime { get; set; }     // وقت التعديل (مهم جداً)
        public string? TraPatRefId { get; set; }     // رابط السب بيس (مهم جداً)

        public string? TraTitle { get; set; }
        public string? TraDateId { get; set; }
        public string? TraTimeId { get; set; }
        public string? TraPatId { get; set; }        // رقم العميل
        public string? TraTot { get; set; }          // الإجمالي
        public string? TraDis { get; set; }          // الخصم
        public string? TraTax { get; set; }          // الضريبة
        public string? TraNet { get; set; }          // الصافي
        public string? TraCurId { get; set; }        // العملة
        public string? TraPatName { get; set; }      // اسم العميل
        public string? TraStoreId { get; set; }      // المخزن
        public string? TrauId { get; set; }          // المستخدم

        public string? TraAccId { get; set; }        // الحساب (الصندوق/العميل)
        
        // --- Added for Reverse Sync ---
        public string? TRACOLID { get; set; }        // المحصل
        public string? TRADELID { get; set; }        // المندوب
    }
}