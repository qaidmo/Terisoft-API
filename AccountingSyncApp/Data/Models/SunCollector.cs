// 💡 ملف SunCollector.cs

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AccountingSyncAPP.Data.Models
{
    [Table("SUNCOLLECTOR")]
    public class SunCollector
    {
        [Key]
        [Column("COLID")]
        public string? ColId { get; set; } 

        [Column("COLNAME")]
        public string? ColName { get; set; } 

        [Column("COLMOBILNO")]
        public string? ColMobilNo { get; set; } 

        [Column("COLACCID")]
        public string? ColAccId { get; set; } 

        [Column("COLFID")]
        public string? ColFId { get; set; } 

        [Column("COLSTATE")]
        public string? ColState { get; set; } 

        [Column("EDTDATE")]
        public string? EdtDate { get; set; } 
    }
}