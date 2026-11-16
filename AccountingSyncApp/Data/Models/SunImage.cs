using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AccountingSyncAPP.Data.Models
{
    [Table("SUNIMAGES")]
    public class SunImage
    {
        [Key]
        [Column("IMGID")]
        public string ImgId { get; set; } = null!; // رقم الصورة (مثل 021021)

        [Column("IMGIMAGE")]
        public byte[]? ImgData { get; set; } // البيانات الثنائية للصورة
    }
}