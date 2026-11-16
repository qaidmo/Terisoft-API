using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AccountingSyncAPP.Data.Models
{
    [Table("SUNITEMGROUPMAIN")]
    public class SunItemGroupMain
    {
        [Key]
        [Column("ITMGRPMINID")]
        public string? ItmGrpMinId { get; set; }

        [Column("ITMGRPMINNAME")]
        public string? ItmGrpMinName { get; set; }

        [Column("ITMGRPMINSTATE")]
        public string? ItmGrpMinState { get; set; }
        [Column("EDTDATE")]
        public string? EdtDate { get; set; }
    }
}