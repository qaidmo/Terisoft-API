using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AccountingSyncAPP.Data.Models
{
    [Table("SUNITEM")]
    public class SunItem
    {
        [Key]
        [Column("ITMID")]
        public string? ItmId { get; set; }

        [Column("ITMNO")]
        public string? ItmNo { get; set; }

        [Column("ITMNAME")]
        public string? ItmName { get; set; }

        [Column("ITMNAMEE")]
        public string? ItmNamee { get; set; }

        [Column("ITMBARCODE")]
        public string? ItmBarcode { get; set; }

        [Column("ITMUNTNAME")]
        public string? ItmUntName { get; set; }

        [Column("ITMSALE")]
        public string? ItmSale { get; set; }

        [Column("GRPID")]
        public string? GrpId { get; set; }

        [Column("ITMGRPID")]
        public string? ItmGrpId { get; set; }

        [Column("ITMCURSID")]
        public string? ItmCursId { get; set; }

        [Column("ITMACCID")]
        public string? ItmAccId { get; set; }

        [Column("ITMACCCOST")]
        public string? ItmAccCost { get; set; }

        [Column("ITMACCINCOME")]
        public string? ItmAccIncome { get; set; }

        [Column("ITMACCDISC")]
        public string? ItmAccDisc { get; set; }

        [Column("ITMSTATE")]
        public string? ItmState { get; set; }

        [Column("ITMNAOTHER")]
        public string? ItmNaOther { get; set; }

        [Column("ITMCCID")]
        public string? ItmCcId { get; set; }

        [Column("ITMOPTIONS")]
        public string? ItmOptions { get; set; }

        [Column("EDTDATE")]
        public string? EdtDate { get; set; }

        [Column("ITMCOST")]
        public string? ItmCost { get; set; }

        [Column("ITMCOST3")]
        public string? ItmCost3 { get; set; }

        [Column("ITMSECID")]
        public string? ItmSecId { get; set; }

        [Column("ITMCOMID")]
        public string? ItmComId { get; set; }

        [Column("ITMTYID")]
        public string? ItmTyId { get; set; }

        [Column("ITMGRPMINID")]
        public string? ItmGrpMinId { get; set; }

        [Column("ITMINDEX")]
        public string? ItmIndex { get; set; }

        [Column("ITMACCBONUS")]
        public string? ItmAccBonus { get; set; }

        [Column("ITMACCTAX")]
        public string? ItmAccTax { get; set; }

        [Column("ITMACCTAXOTH")]
        public string? ItmAccTaxOth { get; set; }

        [Column("ITMACCOTH")]
        public string? ItmAccOth { get; set; }

        [Column("ITMUNTMAXSIZNAME")]
        public string? ItmUntMaxSizName { get; set; }

        [Column("ITMUNTMINSIZNAME")]
        public string? ItmUntMinSizName { get; set; }

        [Column("ITMUNTMAXSIZVALUE")]
        public string? ItmUntMaxSizValue { get; set; }

        [Column("ITMUNTMINSIZVALUE")]
        public string? ItmUntMinSizValue { get; set; }

        [Column("ITMSHURTCODE")]
        public string? ItmShurtCode { get; set; }

        [Column("ITMSGRPNAME")]
        public string? ItmSgrpName { get; set; }

        [Column("ITMUNTMAXSIZID")]
        public string? ItmUntMaxSizId { get; set; }

        [Column("ITMUNTMINSIZID")]
        public string? ItmUntMinSizId { get; set; }

        [Column("ITMUNTSIZSALE")]
        public string? ItmUntSizSale { get; set; }
    }
}