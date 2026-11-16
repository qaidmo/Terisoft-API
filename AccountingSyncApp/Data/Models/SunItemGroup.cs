using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AccountingSyncAPP.Data.Models
{
    [Table("SUNITEMGROUP")]
    public class SunItemGroup
    {
        [Key]
        [Column("ITMGRPID")]
        public string? ItmGrpId { get; set; }

        [Column("ITMGRPNAME")]
        public string? ItmGrpName { get; set; }

        [Column("ITMGRPSTATE")]
        public string? ItmGrpState { get; set; }

        [Column("ITMGRPMINID")]
        public string? ItmGrpMinId { get; set; }

        [Column("ITMGRPLEN")]
        public string? ItmGrpLen { get; set; }

        [Column("EDTDATE")]
        public string? EdtDate { get; set; }
    }
}