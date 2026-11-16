using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

[Table("SUNTRANSDETAILITEM1")]
public class SunTransDetailItem1
{
    [Key]
    [Column("TRADID")]
    public string TradId { get; set; }

    [Column("TRAID")]
    public string TraId { get; set; }

    // --- الإضافة الجديدة هنا ---
    [Column("TRADTRADATEID")]
    public string TradTraDateId { get; set; }

    [Column("TRADTRATIMEID")]
    public string TradTraTimeId { get; set; }

    [Column("TRADLISTID")]
    public string TradListId { get; set; }
    // --- نهاية الإضافة ---

    [Column("TRADINDEX")]
    public string? TradIndex { get; set; }

    [Column("TRADITMID")]
    public string? TradItmId { get; set; }

    [Column("TRADITMNAME")]
    public string? TradItmName { get; set; }

    [Column("TRADITMNAMEE")]
    public string TradItmNameE { get; set; }

    [Column("TRADITMUNIT")]
    public string TradItmUnit { get; set; }

    [Column("TRADUNITSIZE")]
    public string TradUnitSize { get; set; }

    [Column("TRADSTORID")]
    public string TradStorId { get; set; }

    [Column("TRADQTY")]
    public string TradQty { get; set; }

    [Column("TRADQTYDIS")]
    public string TradQtyDis { get; set; }

    [Column("TRADQTYOTH")]
    public string? TradQtyOth { get; set; }

    [Column("TRADQTYTOT")]
    public string TradQtyTot { get; set; }

    [Column("TRADUNTVALUE")]
    public string TradUntValue { get; set; }

    [Column("TRADUNTDIS")]
    public string TradUntDis { get; set; }

    [Column("TRADUNTDISPER")]
    public string TradUntDisPer { get; set; }

    [Column("TRADUNTTAX")]
    public string TradUntTax { get; set; }

    [Column("TRADUNTTAXPER")]
    public string TradUntTaxPer { get; set; }

    [Column("TRADITMCURID")]
    public string TradItmCurId { get; set; }

    [Column("TRADITMCURVALUE")]
    public string TradItmCurValue { get; set; }

    [Column("TRADTRACURVALUE")]
    public string TradTraCurValue { get; set; }

    [Column("TRADITMUNTVALUE")]
    public string TradItmUntValue { get; set; }

    [Column("TRADAMOUNT")]
    public string TradAmount { get; set; }

    [Column("TRADNETVALUE")]
    public string TradNetValue { get; set; }

    [Column("TRADNETAMOUNT")]
    public string TradNetAmount { get; set; }

    [Column("TRADREFID")]
    public string? TradRefId { get; set; }

    [Column("TRADEXPIREID")]
    public string TradExpireId { get; set; }

    [Column("TRADEXPIRE")]
    public string? TradExpire { get; set; }

    [Column("TRADMODEL")]
    public string? TradModel { get; set; }

    [Column("TRADX")]
    public string? TradX { get; set; }

    [Column("TRADY")]
    public string? TradY { get; set; }

    [Column("TRADWEIGHT")]
    public string? TradWeight { get; set; }

    [Column("TRADREMARK")]
    public string? TradRemark { get; set; }

    [Column("TRADSIGND")]
    public string TradSignd { get; set; }

    [Column("TRADSIGNDIN")]
    public string TradSigndIn { get; set; }

    [Column("TRADSIGNDOUT")]
    public string TradSigndOut { get; set; }

    [Column("TRADSIGNDCOST")]
    public string TradSigndCost { get; set; }

    [Column("TRADSIGNDREP")]
    public string TradSigndRep { get; set; }

    [Column("TRADBRAID")]
    public string? TradBraId { get; set; }

    [Column("TRADITMUNTCOST")]
    public string? TradItmUntCost { get; set; }

    [Column("TRADSIGNDRCV")]
    public string? TradSigndRcv { get; set; }

    [Column("TRADSTORESHILF")]
    public string? TradStoreShilf { get; set; }

    [Column("TRADOTH")]
    public string? TradOth { get; set; }

    [Column("TRADBONSIGND")]
    public string TradBonSignd { get; set; }

    [Column("TRADPARTNUMBER")]
    public string? TradPartNumber { get; set; }

    [Column("TRADCLASSF")]
    public string? TradClassF { get; set; }

    [Column("TRADCODE")]
    public string? TradCode { get; set; }

    [Column("TRADFACTORY")]
    public string? TradFactory { get; set; }

    [Column("TRADNAME")]
    public string? TradName { get; set; }

    [Column("TRADCITY")]
    public string? TradCity { get; set; }

    [Column("TRADCURCHAR")]
    public string? TradCurChar { get; set; }

    [Column("TRADPERLINE")]
    public string? TradPerLine { get; set; }

    [Column("TRADINFO")]
    public string? TradInfo { get; set; }

    [Column("TRADCCID")]
    public string? TradCcId { get; set; }

    [Column("TRADCCID2")]
    public string? TradCcId2 { get; set; }

    [Column("TRADTRADISC")]
    public string? TradTraDisc { get; set; }

    [Column("TRADTRATAX")]
    public string? TradTraTax { get; set; }

    [Column("TRADSYNC")]
    public string? TradSync { get; set; }
}