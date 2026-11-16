using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

[Table("SUNACTYPE")]
public class SunAcType
{
    [Key]
    [Column("ACTYID")]
    public string Actyid { get; set; }

    [Column("ACMAID")]
    public string Acmaid { get; set; }

    [Column("ACTYNAME")]
    public string Actyname { get; set; }

    [Column("ACTYNAMEE")]
    public string Actynamee { get; set; }
}