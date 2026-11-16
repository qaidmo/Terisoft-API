using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AccountingSyncAPP.Data.Models
{
    [Table("ALMAHTA")]
    public class Almahta
    {
        [Key]
        [Column("M_NO")]
        public string MNo { get; set; }

        [Column("M_NAME")]
        public string MName { get; set; }

        [Column("M_RIGHT")]
        public string MRight { get; set; }

        [Column("DATE_M_RIGHT")]
        public string DateMRight { get; set; }
    }
}
