using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AccountingSyncAPP.Data.Models
{
    [Table("SUNCCDIR")]
    public class SunCostCenter
    {
        [Key]
        [Column("ACCID")]
        public string? AccId { get; set; }

        [Column("ACCNAME")]
        public string? AccName { get; set; }

        [Column("ACCSTATE")]
        public string? AccState { get; set; }

        [Column("ACCEDTDATE")]
        public string? AccEdtDate { get; set; }
    }
}