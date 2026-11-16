using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

[Table("SUNTRANSHEAD1")]
public class SunTransHead1
{
    [Key]
    [Column("TRAID")]
    public string TraId { get; set; }

    [Column("TRALSTID")]
    public string TraLstId { get; set; }

    [Column("TRAYEARID")]
    public string TraYearId { get; set; }

    [Column("TRASYSID")]
    public string TraSysId { get; set; }

    [Column("TRABRAID")]
    public string TraBraId { get; set; }

    [Column("TRACOMID")]
    public string TraComId { get; set; }

    [Column("TRASECID")]
    public string TraSecId { get; set; }

    [Column("TRAPOSID")]
    public string TraPosId { get; set; }

    [Column("TRAINDEX")]
    public string TraIndex { get; set; }

    [Column("TRANO")]
    public string TraNo { get; set; }

    [Column("TRADATEID")]
    public string TraDateId { get; set; }

    [Column("TRATIMEID")]
    public string TraTimeId { get; set; }

    [Column("TRADATETIMEID")]
    public string TraDateTimeId { get; set; }

    [Column("TRADATETIME")]
    public string TraDateTime { get; set; }

    [Column("TRADATE")]
    public string TraDate { get; set; }

    [Column("TRASTATUSDEL")]
    public string TraStatusDel { get; set; }

    [Column("TRATITLE")]
    public string? TraTitle { get; set; }

    [Column("TRASYNC")]
    public string? TraSync { get; set; }
}