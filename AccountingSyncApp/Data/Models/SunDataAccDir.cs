using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AccountingSyncAPP.Data.Models
{
    [Table("SUNACCDIR")]
    public class SunDataAccDir
    {
        [Key]
        [Column("ACCID")]
        public string AccId { get; set; }

        [Column("ACCNAME")]
        public string AccName { get; set; }

        [Column("ACCNAMEE")]
        public string? AccNameE { get; set; }

        [Column("ACCFID")]
        public string? AccFid { get; set; }

        [Column("ACCLVID")]
        public string? AccLvid { get; set; }

        [Column("ACCTRASTATE")]
        public string? AccTraState { get; set; }

        [Column("ACCSTATE")]
        public string? AccState { get; set; }

        [Column("ACCNETURAL")]
        public string? AccNetural { get; set; }

        [Column("ACCMAID")]
        public string? AccMaid { get; set; }

        [Column("ACTYDEID")]
        public string? ActyDeId { get; set; }

        [Column("ACTYID")]
        public string? ActyId { get; set; }

        // --- ✨ الإضافة المطلوبة لمزامنة العملة الافتراضية ✨ ---
        [Column("ACCDEFCURID")]
        public string? AccDefCurId { get; set; }
    }
}