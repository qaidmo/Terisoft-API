using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AccountingSyncAPP.Data.Models
{
    [Table("SUNBRANCH")]
    public class SunBranch
    {
        [Key]
        [Column("BRAID")]
        public string? BraId { get; set; }

        [Column("BRANAME")]
        public string? BraName { get; set; }

        [Column("BRASTATE")]
        public string? BraState { get; set; }

        // ✅ إضافة: الاسم العربي للشركة (الترويسة)
        [Column("BRAHEADERR")]
        public string? BraHeaderR { get; set; }

        // ✅ إضافة: عنوان الفرع
        [Column("BRAADDRESS")]
        public string? BraAddress { get; set; }
    }
}