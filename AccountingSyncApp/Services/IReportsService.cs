using AccountingSyncAPP.Data.Dtos;
using System.Threading.Tasks;

namespace AccountingSyncAPP.Services
{
    public interface IReportsService
    {
        Task<AccountStatementResponseDto> GetAccountStatementAsync(AccountStatementRequestDto request);
        Task<AccountStatementResponseDto> GetDeltaAccountStatementAsync(AccountStatementDeltaRequestDto request);

        Task<TransactionHistoryResponseDto> GetTransactionHistoryAsync(TransactionHistoryRequestDto request);

        Task<CustomerActivityResponseDto> GetCustomerActivitySummaryAsync(CustomerActivityRequestDto request);
        Task<CostCenterReportResponseDto> GetCostCenterTransactionsAsync(CostCenterReportRequestDto request);

        // تقرير حركة المبيعات الجديد - مستقل تماماً
        Task<SalesMovementResponseDto> GetSalesMovementAsync(SalesMovementRequestDto request);

        // تقرير حركة الموردين - مستقل تماماً
        Task<SupplierMovementResponseDto> GetSupplierMovementAsync(SupplierMovementRequestDto request);
    }
}