using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AccountingSyncAPP.Data.Models
{
    [Table("SUNVENDER")]
    public class SunVender
    {
        [Key]
        [Column("VNDRID")]
        public string? VndrId { get; set; }

        [Column("VNDRNAME")]
        public string? VndrName { get; set; }

        [Column("VNDRNAMEE")]
        public string? VndrNameE { get; set; }

        [Column("VNDRFID")]
        public string? VndrFid { get; set; }

        [Column("VNDRADDRESS")]
        public string? VndrAddress { get; set; }

        [Column("VNDRADDRESSE")]
        public string? VndrAddressE { get; set; }

        [Column("VNDRTOWN")]
        public string? VndrTown { get; set; }

        [Column("VNDRBOBOX")]
        public string? VndrBobox { get; set; }

        [Column("VNDRPHONE")]
        public string? VndrPhone { get; set; }

        [Column("VNDREMAIL")]
        public string? VndrEmail { get; set; }

        [Column("VNDRWEB")]
        public string? VndrWeb { get; set; }

        [Column("VNDRREMARKS")]
        public string? VndrRemarks { get; set; }

        [Column("VNDROPTIONS")]
        public string? VndrOptions { get; set; }

        [Column("VNDRACCID")]
        public string? VndrAccId { get; set; }

        [Column("VNDRACCIDFID")]
        public string? VndrAccIdFid { get; set; }

        [Column("VNDRSTATE")]
        public string? VndrState { get; set; }

        [Column("VNDRCHAR")]
        public string? VndrChar { get; set; }

        [Column("VNDRMAXNO")]
        public string? VndrMaxNo { get; set; }

        [Column("EDTDATE")]
        public string? EdtDate { get; set; }

        [Column("VNDRMOBILNO")]
        public string? VndrMobilNo { get; set; }

        [Column("VNDRFAXNO")]
        public string? VndrFaxNo { get; set; }

        [Column("VNDRAGRPACCID")]
        public string? VndrAgrpAccId { get; set; }

        [Column("VNDRAGRPID")]
        public string? VndrAgrpId { get; set; }

        [Column("VNDRAGRPNAME")]
        public string? VndrAgrpName { get; set; }

        [Column("SYNCFLAG")]
        public string? SyncFlag { get; set; }
    }
}
