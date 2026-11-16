using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

[Table("SUNACMAIN")]
public class SunAcMain
{
    [Key]
    [Column("ACMAID")]
    public string? Acmaid { get; set; }

    [Column("ACMANAME")]
    public string? Acmaname { get; set; }

    [Column("ACMANAMEE")]
    public string? Acmanamee { get; set; }

    [Column("ACMAREPID")]
    public string? Acmarepid { get; set; }

    [Column("ACMASIG")]
    public string? Acmasig { get; set; }
}