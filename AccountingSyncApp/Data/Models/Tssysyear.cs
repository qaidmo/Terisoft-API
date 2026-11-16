using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AccountingSyncAPP.Data.Models
{
    [Table("TSSYSYEAR")]
    public class Tssysyear
    {
        [Key]
        [Column("YRID")]
        public string Yrid { get; set; }

        [Column("YRVALUE")]
        public string Yrvalue { get; set; }

        [Column("YRMINDATE")]
        public string Yrmindate { get; set; }

        [Column("YRMAXDATE")]
        public string Yrmaxdate { get; set; }

        [Column("YRSTATUS")]
        public string Yrstatus { get; set; }
    }
}