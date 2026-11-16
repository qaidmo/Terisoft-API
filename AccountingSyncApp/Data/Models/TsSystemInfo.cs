using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AccountingSyncAPP.Data.Models
{
    [Table("TSSYSTEMINFO")]
    public class TsSystemInfo
    {
        [Key]
        [Column("SYSID")]
        public string SysId { get; set; }

        [Column("SYSYEARSTART")]
        public string SysYearStart { get; set; }

    }
}