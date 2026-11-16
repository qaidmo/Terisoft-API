// 💡 ملف SunDealer.cs

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AccountingSyncAPP.Data.Models
{
    [Table("SUNDEALER")]
    public class SunDealer
    {
        [Key]
        [Column("DELID")]
        public string? DelId { get; set; }

        [Column("DELNAME")]
        public string? DelName { get; set; }

        [Column("DELMOBILNO")]
        public string? DelMobilNo { get; set; }

        [Column("DELACCID")]
        public string? DelAccId { get; set; }

        [Column("DELMAXNO")]
        public string? DelMaxNo { get; set; }

        [Column("DELPERSTATE")]
        public string? DelPerState { get; set; } // حالة العمولات

        [Column("DELOTHER2")]
        public string? DelOther2 { get; set; } // رقم حساب المصروفات

        [Column("DELSTATE")]
        public string? DelState { get; set; }

        [Column("EDTDATE")]
        public string? EdtDate { get; set; }
    }
}