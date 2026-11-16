using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

[Table("SUNTRANSHEADLINE1")]
public class SunTransHeadline1
{
    [Key]
    [Column("TRAID")]
    public string TraId { get; set; }

    [Column("TRATITLE")]
    public string? TraTitle { get; set; }

    [Column("TRAREPTITLE")]
    public string? TraRepTitle { get; set; }

    [Column("TRAREFR")]
    public string? TraRefr { get; set; }

    [Column("TRACURID")]
    public string? TraCurId { get; set; }

    [Column("TRACURVALUE")]
    public string? TraCurValue { get; set; }

    [Column("TRAACCID")]
    public string? TraAccId { get; set; }

    [Column("TRAACCNAME")]
    public string? TraAccName { get; set; }

    [Column("TRAPATCOMID")]
    public string? TraPatComId { get; set; }

    [Column("TRAPATCOMNAME")]
    public string? TraPatComName { get; set; }

    [Column("TRAPATID")]
    public string? TraPatId { get; set; }

    [Column("TRAPATNAME")]
    public string? TraPatName { get; set; }

    [Column("TRAPATAGE")]
    public string? TraPatAge { get; set; }

    [Column("TRAPATSEXID")]
    public string? TraPatSexId { get; set; }

    [Column("TRAPATSEXNAME")]
    public string? TraPatSexName { get; set; }

    [Column("TRAPATBDATE")]
    public string? TraPatBDate { get; set; }

    [Column("TRAPATREFID")]
    public string? TraPatRefId { get; set; }

    [Column("TRAPATREFEXPIRE")]
    public string? TraPatRefExpire { get; set; }

    [Column("TRAPATADDRESS")]
    public string? TraPatAddress { get; set; }

    [Column("TRAPATCITY")]
    public string? TraPatCity { get; set; }

    [Column("TRAPATCODEOK")]
    public string? TraPatCodeOk { get; set; }

    [Column("TRAPATDIAG")]
    public string? TraPatDiag { get; set; }

    [Column("TRAPATPERVAL")]
    public string? TraPatPerVal { get; set; }

    [Column("TRAPATPERAMOUNT")]
    public string? TraPatPerAmount { get; set; }

    [Column("TRASIGND")]
    public string? TraSignd { get; set; }

    [Column("TRAPATPAID")]
    public string? TraPatPaid { get; set; }

    [Column("TRAPATTOTPAID")]
    public string? TraPatTotPaid { get; set; }

    [Column("TRATOT")]
    public string? TraTot { get; set; }

    [Column("TRATAX")]
    public string? TraTax { get; set; }

    [Column("TRADIS")]
    public string? TraDis { get; set; }

    [Column("TRASRV")]
    public string? TraSrv { get; set; }

    [Column("TRANET")]
    public string? TraNet { get; set; }

    [Column("TRATCK")]
    public string? TraTck { get; set; }

    [Column("TRACBN")]
    public string? TraCbn { get; set; }

    [Column("TRAPERDIS")]
    public string? TraPerDis { get; set; }

    [Column("TRAPERTAX")]
    public string? TraPerTax { get; set; }

    [Column("TRAPERSRV")]
    public string? TraPerSrv { get; set; }

    [Column("TRASTOREID")]
    public string? TraStoreId { get; set; }

    [Column("TRASTORENAME")]
    public string? TraStoreName { get; set; }

    [Column("TRAZONID")]
    public string? TraZonId { get; set; }

    [Column("TRAZONNAME")]
    public string? TraZonName { get; set; }

    [Column("TRACOLID")]
    public string? TraColId { get; set; }

    [Column("TRACOLNAME")]
    public string? TraColName { get; set; }

    [Column("TRADELID")]
    public string? TraDelId { get; set; }

    [Column("TRADELNAME")]
    public string? TraDelName { get; set; }

    [Column("TRADCTID")]
    public string? TraDctId { get; set; }

    [Column("TRADCTNAME")]
    public string? TraDctName { get; set; }

    [Column("TRACCID")]
    public string? TraCcId { get; set; }

    [Column("TRACCNAME")]
    public string? TraCcName { get; set; }

    [Column("TRACHAR")]
    public string? TraChar { get; set; }

    [Column("TRAUID")]
    public string? TrauId { get; set; }

    [Column("TRAUNAME")]
    public string? TrauName { get; set; }

    [Column("TRACHEEKID")]
    public string? TraCheekId { get; set; }

    [Column("TRACHEEKDATE")]
    public string? TraCheekDate { get; set; }

    [Column("TRACHEEKDATEID")]
    public string? TraCheekDateId { get; set; }

    [Column("TRASHIPDAYS")]
    public string? TraShipDays { get; set; }

    [Column("TRASHIPDATE")]
    public string? TraShipDate { get; set; }

    [Column("TRASHIPDATEID")]
    public string? TraShipDateId { get; set; }

    [Column("TRASHIPNOTE")]
    public string? TraShipNote { get; set; }

    [Column("TRASHIPLNG")]
    public string? TraShipLng { get; set; }

    [Column("TRASHIPNO")]
    public string? TraShipNo { get; set; }

    [Column("TRACHKUSR1")]
    public string? TraChkUsr1 { get; set; }

    [Column("TRACHKUSR2")]
    public string? TraChkUsr2 { get; set; }

    [Column("TRACHKUSR3")]
    public string? TraChkUsr3 { get; set; }

    [Column("TRACHKUSR4")]
    public string? TraChkUsr4 { get; set; }

    [Column("TRACHKUSR5")]
    public string? TraChkUsr5 { get; set; }

    [Column("TRAREMARKS")]
    public string? TraRemarks { get; set; }

    [Column("TRANETLET")]
    public string? TraNetLet { get; set; }

    [Column("TRASTATUS")]
    public string? TraStatus { get; set; }

    [Column("TRAPRINTSTATUS")]
    public string? TraPrintStatus { get; set; }

    [Column("TRAPRINTINFO")]
    public string? TraPrintInfo { get; set; }

    [Column("TRALASTEDITUSRID")]
    public string? TraLastEditUsrId { get; set; }

    [Column("TRALASTEDITUSRNAME")]
    public string? TraLastEditUsrName { get; set; }

    [Column("TRALSTID")]
    public string? TraLstId { get; set; }

    [Column("TRAYEARID")]
    public string? TraYearId { get; set; }

    [Column("TRASYSID")]
    public string? TraSysId { get; set; }

    [Column("TRABRAID")]
    public string? TraBraId { get; set; }

    [Column("TRACOMID")]
    public string? TraComId { get; set; }

    [Column("TRASECID")]
    public string? TraSecId { get; set; }

    [Column("TRAPOSID")]
    public string? TraPosId { get; set; }

    [Column("TRAINDEX")]
    public string? TraIndex { get; set; }

    [Column("TRANO")]
    public string? TraNo { get; set; }

    [Column("TRADATEID")]
    public string? TraDateId { get; set; }

    [Column("TRADATE")]
    public string? TraDate { get; set; }

    [Column("TRADATETIME")]
    public string? TraDateTime { get; set; }

    [Column("TRATIMEID")]
    public string? TraTimeId { get; set; }

    [Column("TRADATETIMEID")]
    public string? TraDateTimeId { get; set; }

    [Column("TRASTATUSDEL")]
    public string TraStatusDel { get; set; }

    [Column("TRAOPTIONS")]
    public string? TraOptions { get; set; }

    [Column("TRACLASSID")]
    public string? TraClassId { get; set; }

    [Column("TRACLASSNAME")]
    public string? TraClassName { get; set; }
    
}