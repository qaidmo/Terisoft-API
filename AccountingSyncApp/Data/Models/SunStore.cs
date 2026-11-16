using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AccountingSyncAPP.Data.Models
{
    [Table("SUNSTORE")]
    public class SunStore
    {
        [Key]
        [Column("STRID")]
        public string StrId { get; set; }

        [Column("STRNAME")]
        public string StrName { get; set; }

        [Column("STRACCID")]
        public string? StrAccId { get; set; }

        [Column("STRSTATE")]
        public string? StrState { get; set; }

  
        [Column("STRFID")]
        public string? StrFid { get; set; }
    }
}