using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AccountingSyncAPP.Data.Models
{
    [Table("SUNCURY")]
    public class SunCurrency
    {
        [Key]
        [Column("CURID")]
        public string? CurId { get; set; }

        [Column("CURNAME")]
        public string? CurName { get; set; }

        [Column("CURPARTNAME")]
        public string? CurPartName { get; set; }

        [Column("CURCHAR")]
        public string? CurChar { get; set; }

        [Column("CURVALUE")]
        public string? CurValue { get; set; }

        [Column("CURMAXVALUE")]
        public string? CurMaxValue { get; set; }

        [Column("CURMINVALUE")]
        public string? CurMinValue { get; set; }

        [Column("CURSTATUS")]
        public string? CurStatus { get; set; }
    }
}