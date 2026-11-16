using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AccountingSyncAPP.Data.Models
{
    // تحديد اسم الجدول في قاعدة البيانات
    [Table("SUNTRANSACC1")]
    public class SunTransAcc1
    {
        // المفتاح الأساسي للجدول
        [Key]
        [Column("TRADID")]
        public string? TradId { get; set; }

        [Column("TRAID")]
        public string? TraId { get; set; }

        [Column("TRADINDEX")]
        public string? TradIndex { get; set; }

        [Column("TRADSIGNAL")]
        public string? TradSignal { get; set; }

        [Column("TRADSIGDBT")]
        public string? TradSigDbt { get; set; }

        [Column("TRADSIGCDT")]
        public string? TradSigCdt { get; set; }

        [Column("TRADACCID")]
        public string? TradAccId { get; set; }

        [Column("TRADACCNAME")]
        public string? TradAccName { get; set; }

        [Column("TRADTACCID")]
        public string? TradTAccId { get; set; }

        [Column("TRADTACCNAME")]
        public string? TradTAccName { get; set; }

        [Column("TRADHACCID")]
        public string? TradHAccId { get; set; }

        [Column("TRADHACCNAME")]
        public string? TradHAccName { get; set; }

        [Column("TRADDATNO")]
        public string? TradDatNo { get; set; }

        [Column("TRADDATID")]
        public string? TradDatId { get; set; }

        [Column("TRADDATNAME")]
        public string? TradDatName { get; set; }

        [Column("TRADCLASSID")]
        public string? TradClassId { get; set; }

        [Column("TRADCLASS")]
        public string? TradClass { get; set; }

        [Column("TRADDETAIL")]
        public string? TradDetail { get; set; }

        [Column("TRADQTY")]
        public string? TradQty { get; set; }

        [Column("TRADAMOUNT")]
        public string? TradAmount { get; set; }

        [Column("TRADTAX")]
        public string? TradTax { get; set; }

        [Column("TRADTAXN")]
        public string? TradTaxN { get; set; }

        [Column("TRADTAXACC")]
        public string? TradTaxAcc { get; set; }

        [Column("TRADDIS")]
        public string? TradDis { get; set; }

        [Column("TRADDISN")]
        public string? TradDisN { get; set; }

        [Column("TRADDISACC")]
        public string? TradDisAcc { get; set; }

        [Column("TRADPRICE")]
        public string? TradPrice { get; set; }

        [Column("TRADACCCURCHAR")]
        public string? TradAccCurChar { get; set; }

        [Column("TRADACCCURID")]
        public string? TradAccCurId { get; set; }

        [Column("TRADACCCURVALUE")]
        public string? TradAccCurValue { get; set; }

        [Column("TRADDEBIT")]
        public string? TradDebit { get; set; }

        [Column("TRADCREDIT")]
        public string? TradCredit { get; set; }

        [Column("TRADREF")]
        public string? TradRef { get; set; }

        [Column("TRADREFOTHER")]
        public string? TradRefOther { get; set; }

        [Column("TRADCCID")]
        public string? TradCcId { get; set; }

        [Column("TRADCCNAME")]
        public string? TradCcName { get; set; }

        [Column("TRADSHEEQNO")]
        public string? TradSheeqNo { get; set; }

        [Column("TRADSHEEQDATE")]
        public string? TradSheeqDate { get; set; }

        [Column("TRADSHEEQDATEID")]
        public string? TradSheeqDateId { get; set; }

        // --- أعمدة الأسعار الإضافية ---
        [Column("TRADPRICE1")]
        public string? TradPrice1 { get; set; }

        [Column("TRADPRICE2")]
        public string? TradPrice2 { get; set; }

        [Column("TRADPRICE3")]
        public string? TradPrice3 { get; set; }

        [Column("TRADPRICE4")]
        public string? TradPrice4 { get; set; }

        [Column("TRADPRICE5")]
        public string? TradPrice5 { get; set; }

        [Column("TRADPRICE6")]
        public string? TradPrice6 { get; set; }

        [Column("TRADPRICE7")]
        public string? TradPrice7 { get; set; }

        [Column("TRADPRICE8")]
        public string? TradPrice8 { get; set; }

        [Column("TRADPRICE9")]
        public string? TradPrice9 { get; set; }

        // --- أعمدة المبالغ الإضافية ---
        [Column("TRADAMOUNT1")]
        public string? TradAmount1 { get; set; }

        [Column("TRADAMOUNT2")]
        public string? TradAmount2 { get; set; }

        [Column("TRADAMOUNT3")]
        public string? TradAmount3 { get; set; }

        [Column("TRADAMOUNT4")]
        public string? TradAmount4 { get; set; }

        [Column("TRADAMOUNT5")]
        public string? TradAmount5 { get; set; }

        [Column("TRADAMOUNT6")]
        public string? TradAmount6 { get; set; }

        [Column("TRADAMOUNT7")]
        public string? TradAmount7 { get; set; }

        [Column("TRADAMOUNT8")]
        public string? TradAmount8 { get; set; }

        [Column("TRADAMOUNT9")]
        public string? TradAmount9 { get; set; }

        // --- أعمدة البيانات الأخرى ---
        [Column("TRADOTHER1")]
        public string? TradOther1 { get; set; }

        [Column("TRADOTHER2")]
        public string? TradOther2 { get; set; }

        [Column("TRADOTHER3")]
        public string? TradOther3 { get; set; }

        [Column("TRADOTHER4")]
        public string? TradOther4 { get; set; }

        [Column("TRADOTHER5")]
        public string? TradOther5 { get; set; }

        [Column("TRADOTHER6")]
        public string? TradOther6 { get; set; }

        [Column("TRADOTHER7")]
        public string? TradOther7 { get; set; }

        [Column("TRADOTHER8")]
        public string? TradOther8 { get; set; }

        [Column("TRADOTHER9")]
        public string? TradOther9 { get; set; }

        // --- حقل المزامنة ---
        [Column("SYNCFLAG")]
        public string? SyncFlag { get; set; }
    }
}