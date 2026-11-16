using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AccountingSyncAPP.Data.Models
{
    [Table("SUNSECTION")]
    public class SunSection
    {
        [Key]
        [Column("SECID")]
        public string? SecId { get; set; }

        [Column("SECNAME")]
        public string? SecName { get; set; }

        [Column("SECSTATE")]
        public string? SecState { get; set; } // "0" for active

        [Column("SECHEADERR")]
        public string? SecHeaderR { get; set; }

        [Column("SECADDRESS")]
        public string? SecAddress { get; set; }
    }
}