using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AccountingSyncAPP.Data.Models
{
    [Table("SUNACCDIRCURY")]
    public class SunAccDirCury
    {
        [Key]
        [Column("ACCCURID")]
        public string? AccCurId { get; set; }

        [Column("ACCCID")]
        public string? AccId { get; set; }

        [Column("CURID")]
        public string? CurId { get; set; }

        [Column("ACCCNAME")]
        public string? AccCName { get; set; }

        [Column("ACCCURINDEX")]
        public string? AccCurIndex { get; set; }

        [Column("ACCCURSTATE")]
        public string? AccCurState { get; set; }

        [Column("EDTDATE")]
        public string? EdtDate { get; set; }

        [Column("SYNCFLAG")]
        public string? SyncFlag { get; set; }
    }
}