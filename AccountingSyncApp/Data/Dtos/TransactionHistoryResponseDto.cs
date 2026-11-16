using System.Collections.Generic;

namespace AccountingSyncAPP.Data.Dtos
{
    public class TransactionHistoryResponseDto
    {
        public decimal GrandTotal { get; set; }
        public int ItemsCount { get; set; }
        public List<TransactionHistoryItemDto> Transactions { get; set; }
    }

    public class TransactionHistoryItemDto
    {
        public string TransactionId { get; set; }
        public string TransactionNumber { get; set; }
        public string ReferenceNumber { get; set; }
        public string Date { get; set; }
        public string Time { get; set; }
        public string CustomerName { get; set; }
        public decimal NetTotal { get; set; }
        public string UserName { get; set; }
        public string TransactionTypeId { get; set; }
        public string TransactionTypeName { get; set; }
    }
}