using System;
using System.ComponentModel.DataAnnotations;

namespace AccountingSyncAPP.Data.Dtos
{
    public class AccountStatementDeltaRequestDto : AccountStatementRequestDto
    {
        [Required]
        public DateTime LastSyncTimestamp { get; set; }

        public string? LedgerSignature { get; set; }
    }
}
