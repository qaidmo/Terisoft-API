using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AccountingSyncAPP.Data.Models
{
    [Table("SUNDATAACCDIR")]
    public class SunDataAnalyticalAccDir
    {
        [Key]
        [Column("ACCID")]
        public string AccId { get; set; }

        [Column("ACCNAME")]
        public string? AccName { get; set; }

        [Column("ACCNAMEE")]
        public string? AccNameE { get; set; }

        [Column("ACCFID")]
        public string? AccFid { get; set; }

        [Column("GRPID")]
        public string? GrpId { get; set; }

        [Column("ACCSTATE")]
        public string? AccState { get; set; }

        [Column("ACCMAID")]
        public string? Accmaid { get; set; }

        [Column("ACTYID")]
        public string? Actyid { get; set; }

        [Column("ACCSIG")]
        public string? Accsig { get; set; }

        [Column("ACCNAMEB")]
        public string? AccNameB { get; set; }

        [Column("ACCLVID")]
        public string? AccLvid { get; set; }

        [Column("ACCNO")]
        public string? AccNo { get; set; }

        [Column("ACCNETURAL")]
        public string? AccNetural { get; set; }

        [Column("ACCREPID")]
        public string? AccRepId { get; set; }

        [Column("ACTYDEID")]
        public string? ActyDeId { get; set; }

        [Column("ACCINDEX")]
        public string? AccIndex { get; set; }

        [Column("ACCGRPID")]
        public string? AccGrpId { get; set; }

        [Column("ACCGRPNAME")]
        public string? AccGrpName { get; set; }

        [Column("ACCCHAR")]
        public string? AccChar { get; set; }

        [Column("ACCTRASTATE")]
        public string? AccTraState { get; set; }

        [Column("ACCDEFCURID")]
        public string? AccDefCurId { get; set; }

        [Column("ACCHIDE")]
        public string? AccHide { get; set; }

        [Column("ACCEDTDATE")]
        public string? AccEdtDate { get; set; }

        [Column("ACCSYNCFLAG")]
        public string? AccSyncFlag { get; set; }

        [Column("ACCMAXNO")]
        public string? AccMaxNo { get; set; }

        [Column("ACCEVERYONE")]
        public string? AccEveryone { get; set; }
    }
}