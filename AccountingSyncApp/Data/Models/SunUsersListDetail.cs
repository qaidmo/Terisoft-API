using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AccountingSyncAPP.Data.Models
{
    [Table("SUNUSERSLISTDETAIL")]
    public class SunUsersListDetail
    {
        [Key]
        [Column("ULDID")]
        public string UldId { get; set; }

        [Column("LISTDID")]
        public string ListId { get; set; }

        [Column("UID")]
        public string? UserId { get; set; }

        [Column("ULDENTER")]
        public string? CanEnter { get; set; }

        [Column("ULDADD")]
        public string? CanAdd { get; set; }

        [Column("ULDEDIT")]
        public string? CanEdit { get; set; }

        [Column("ULDFIND")]
        public string? CanFind { get; set; }

        [Column("ULDPRINT")]
        public string? CanPrint { get; set; }

        [Column("ULDCHGSALE")]
        public string? CanChangeSale { get; set; }

        [Column("ULDINFODATE")]
        public string? InfoDate { get; set; }

        [Column("ULDEDATE")]
        public string? EditDate { get; set; }
    }
}