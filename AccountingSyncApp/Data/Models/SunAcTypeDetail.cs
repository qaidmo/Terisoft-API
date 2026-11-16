using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

[Table("SUNACTYPEDETAIL")]
public class SunAcTypeDetail
{
    [Key]
    [Column("ACTYDEID")]
    public string? Actydeid { get; set; }

    [Column("ACTYID")]
    public string? Actyid { get; set; }

    [Column("ACTYDENAME")]
    public string? Actydename { get; set; }

    [Column("ACTYDENAMEE")]
    public string? Actydenamee { get; set; }

    // يمكنك إضافة بقية الأعمدة إذا احتجتها لاحقًا
}