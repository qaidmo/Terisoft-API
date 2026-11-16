using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

[Table("SUNTRANSJE1")]
public class SunTransJe1
{
    [Key]
    [Column("TRADID")]
    public string TradId { get; set; }

    [Column("TRAID")]
    public string TraId { get; set; }

    [Column("TRADINDEX")]
    public string TradIndex { get; set; }

    [Column("TRADTYPE")]
    public string TradType { get; set; }

    [Column("TRADLSTID")]
    public string TradLstId { get; set; }

    [Column("TRADACCID")]
    public string? TradAccId { get; set; }

    [Column("TRADACCNAME")]
    public string? TradAccName { get; set; }

    [Column("TRADTACCID")]
    public string? TradTAccId { get; set; }

    [Column("TRADTACCNAME")]
    public string? TradTaCcName { get; set; }

    [Column("TRADHACCID")]
    public string? TradHAccId { get; set; }

    [Column("TRADHACCNAME")]
    public string? TradHaCcName { get; set; }

    [Column("TRADDATID")]
    public string? TradDatId { get; set; }

    [Column("TRADDATNAME")]
    public string? TradDatName { get; set; }

    [Column("TRADFACCID")]
    public string? TradFAccId { get; set; }

    [Column("TRADFFACCID")]
    public string? TradFfAccId { get; set; }

    [Column("TRADNACCID")]
    public string? TradNAccId { get; set; }

    [Column("TRADMACCID")]
    public string? TradMAccId { get; set; }

    [Column("TRADCCID")]
    public string? TradCcId { get; set; }

    [Column("TRADCCNAME")]
    public string? TradCcName { get; set; }

    [Column("TRADCLASSID")]
    public string? TradClassId { get; set; }

    [Column("TRADCLASS")]
    public string? TradClass { get; set; }

    [Column("TRADCURID")]
    public string TradCurId { get; set; }

    [Column("TRADVALUE")]
    public string TradValue { get; set; }

    [Column("TRADVALUELOC")]
    public string TradValueLoc { get; set; }

    [Column("TRADVALUETRA")]
    public string TradValueTra { get; set; }

    [Column("TRADCURVALUE")]
    public string TradCurValue { get; set; }

    [Column("TRADSIG")]
    public string TradSig { get; set; }

    [Column("TRADSIGD")]
    public string TradSigD { get; set; }

    [Column("TRADSIGC")]
    public string TradSigC { get; set; }

    [Column("TRADSIGO")]
    public string TradSigO { get; set; }

    [Column("TRADSIGY")]
    public string TradSigY { get; set; }

    [Column("TRADSIGQ")]
    public string TradSigQ { get; set; }

    [Column("TRADSIGS")]
    public string TradSigS { get; set; }

    [Column("TRADDETAIL")]
    public string? TradDetail { get; set; }

    [Column("TRADREFID")]
    public string? TradRefId { get; set; }

    [Column("TRADREFNO")]
    public string? TradRefNo { get; set; }

    [Column("TRADDATE")]
    public string? TradDate { get; set; }

    [Column("TRADDATEID")]
    public string? TradDateId { get; set; }

    [Column("TRADOTHER1")]
    public string? TradOther1 { get; set; }

    [Column("TRADOTHER2")]
    public string? TradOther2 { get; set; }

    [Column("TRADOTHER3")]
    public string? TradOther3 { get; set; }

    [Column("TRADOTHER4")]
    public string? TradOther4 { get; set; }

    [Column("TRADOTHER5")]
    public string? TradOther5 { get; set; }

    [Column("TRADOTHER6")]
    public string? TradOther6 { get; set; }

    [Column("TRADOTHER7")]
    public string? TradOther7 { get; set; }

    [Column("TRADOTHER8")]
    public string? TradOther8 { get; set; }

    [Column("TRADOTHER9")]
    public string? TradOther9 { get; set; }

    [Column("SYNCFLAG")]
    public string? SyncFlag { get; set; }
    
}