using System;
using System.ComponentModel.DataAnnotations;

namespace AccountingSyncAPP.Data.Dtos
{
    public class CostCenterReportRequestDto
    {
        [Required]
        public int CostCenterNo { get; set; }

        [Required]
        public DateTime FromDate { get; set; }

        [Required]
        public DateTime ToDate { get; set; }

        [Required]
        public string Year { get; set; }

        [Required]
        public int BranchId { get; set; }
    }
}
