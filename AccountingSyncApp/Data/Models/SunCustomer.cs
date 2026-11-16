using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AccountingSyncAPP.Data.Models
{
    [Table("SUNCUSTOMER")]
    public class SunCustomer
    {
        [Key]
        [Column("CUSTID")]
        public string? CustId { get; set; }

        [Column("CUSTNAME")]
        public string? CustName { get; set; }

        [Column("CUSTNAMEE")]
        public string? CustNameE { get; set; }

        [Column("CUSTFID")]
        public string? CustFid { get; set; }

        [Column("CUSTADDRESS")]
        public string? CustAddress { get; set; }

        [Column("CUSTADDRESSE")]
        public string? CustAddressE { get; set; }

        [Column("CUSTTOWN")]
        public string? CustTown { get; set; }

        [Column("CUSTBOBOX")]
        public string? CustBoBox { get; set; }

        [Column("CUSTPHONE")]
        public string? CustPhone { get; set; }

        [Column("CUSTEMAIL")]
        public string? CustEmail { get; set; }

        [Column("CUSTWEB")]
        public string? CustWeb { get; set; }

        [Column("CUSTREMARKS")]
        public string? CustRemarks { get; set; }

        [Column("CUSTOPTIONS")]
        public string? CustOptions { get; set; }

        [Column("CUSTACCID")]
        public string? CustAccId { get; set; }

        [Column("CUSTACCIDFID")]
        public string? CustAccidFid { get; set; }

        [Column("CUSTSTATE")]
        public string? CustState { get; set; }

        [Column("CUSTCHAR")]
        public string? CustChar { get; set; }

        [Column("CUSTMAXNO")]
        public string? CustMaxNo { get; set; }

        [Column("EDTDATE")]
        public string? EdtDate { get; set; }

        [Column("CUSTMOBILNO")]
        public string? CustMobilNo { get; set; }

        [Column("CUSTFAXNO")]
        public string? CustFaxNo { get; set; }

        [Column("CUSTAGRPACCID")]
        public string? CustAGrpAccId { get; set; }

        [Column("CUSTAGRPID")]
        public string? CustAGrpId { get; set; }

        [Column("CUSTAGRPNAME")]
        public string? CustAGrpName { get; set; }

        [Column("SYNCFLAG")]
        public string? SyncFlag { get; set; }

        [Column("CUSTCLASS")]
        public string? CustClass { get; set; }

        [Column("CUSTZONE")]
        public string? CustZone { get; set; }

        [Column("CUSTCOLID")]
        public string? CustColId { get; set; }

        [Column("CUSTCOLLECTOR")]
        public string? CustCollector { get; set; }

        [Column("CUSTDEALER")]
        public string? CustDealer { get; set; }

        [Column("CUSTBRANCH")]
        public string? CustBranch { get; set; }

        [Column("CUSTSECTION")]
        public string? CustSection { get; set; }

        [Column("CUSTLIMIT")]
        public string? CustLimit { get; set; }

        [Column("CUSTCURYID")]
        public string? CustCuryId { get; set; }

        [Column("CUSTPERDIS")]
        public string? CustPerDis { get; set; }

        [Column("CUSTBRAID")]
        public string? CustBraId { get; set; }

        [Column("CUSTSECID")]
        public string? CustSecId { get; set; }

        [Column("CUSTLONGX")]
        public string? CustLongX { get; set; }

        [Column("CUSTLONGY")]
        public string? CustLongY { get; set; }

        [Column("CUSTDATEOPEN")]
        public string? CustDateOpen { get; set; }

        [Column("CUSTDATEOPENID")]
        public string? CustDateOpenId { get; set; }

        [Column("CUSTDATECLOSE")]
        public string? CustDateClose { get; set; }

        [Column("CUSTDATECLOSEID")]
        public string? CustDateCloseId { get; set; }

        [Column("CUSTUSERNAME")]
        public string? CustUserName { get; set; }

        [Column("COST_DATA")]
        public string? CostData { get; set; }

        [Column("COST_NEXT")]
        public string? CostNext { get; set; }

        [Column("COST_RSOM")]
        public string? CostRsom { get; set; }
    }
}