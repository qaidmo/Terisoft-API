using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AccountingSyncAPP.Data.Models
{
    [Table("SUNUSERS")]
    public class SunUser
    {
        [Key]
        [Column("UNAME")]
        public string? UName { get; set; }

        [Column("UID")]
        public string? Uid { get; set; }

        [Column("UPASSWORD")]
        public string? UPassword { get; set; }

        [Column("UACCID")]
        public string? UAccId { get; set; }

        [Column("UBRANCHID")]
        public string? UBranchId { get; set; }

        [Column("USECTIONID")]
        public string? USectionId { get; set; }

        [Column("USTOREID")]
        public string? UStoreId { get; set; }
         
        [Column("USTATE")]
        public string? UState { get; set; }
        [Column("EDTDATE")]
        public string? EDTDATE { get; set; }

        [Column("UPRIVDATA")]
        public string? Uprivdata { get; set; }

        [Column("UPRIVWINDOW")]
        public string? Uprivwindow { get; set; }

    }
}