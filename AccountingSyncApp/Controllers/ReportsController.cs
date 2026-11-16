using AccountingSyncAPP.Data.Dtos;
using AccountingSyncAPP.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace AccountingSyncAPP.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ReportsController : ControllerBase
    {
        private readonly IReportsService _reportsService;
        private readonly AccountBalancesService _accountBalancesService;
        private readonly CostCenterProfitMarginService _costCenterProfitMarginService;
        private readonly ILogger<ReportsController> _logger;

        // حقن التبعيات (Dependency Injection)
        public ReportsController(
            IReportsService reportsService,
            AccountBalancesService accountBalancesService,
            CostCenterProfitMarginService costCenterProfitMarginService,
            ILogger<ReportsController> logger)
        {
            _reportsService = reportsService;
            _accountBalancesService = accountBalancesService;
            _costCenterProfitMarginService = costCenterProfitMarginService;
            _logger = logger;
        }

        /// <summary>
        /// جلب كشف حساب بناءً على الفترة والعملة والفرع
        /// </summary>
        [HttpPost("account-statement")]
        public async Task<IActionResult> GetAccountStatement([FromBody] AccountStatementRequestDto request)
        {
            // 1. التحقق من صحة البيانات القادمة
            if (request == null)
            {
                return BadRequest("بيانات الطلب غير صالحة.");
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                // 2. استدعاء السيرفس لجلب البيانات
                var result = await _reportsService.GetAccountStatementAsync(request);

                // 3. إرجاع النتيجة
                return Ok(result);
            }
            catch (Exception ex)
            {
                // تسجيل الخطأ في السيرفر للمراجعة
                _logger.LogError(ex, "حدث خطأ أثناء جلب كشف الحساب للحساب: {AccountId}", request.AccountId);

                // إرجاع رسالة خطأ عامة للموبايل
                return StatusCode(500, new { Message = "حدث خطأ داخلي في السيرفر أثناء معالجة التقرير.", Error = ex.Message });
            }
        }

        [HttpPost("transaction-history")]
        public async Task<IActionResult> GetTransactionHistory([FromBody] TransactionHistoryRequestDto request)
        {
            if (request == null)
            {
                return BadRequest("Invalid request data.");
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var result = await _reportsService.GetTransactionHistoryAsync(request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching transaction history.");
                return StatusCode(500, new { Message = "Internal Server Error", Error = ex.Message });
            }
        }
        [HttpPost("customer-activity")]
        public async Task<IActionResult> GetCustomerActivitySummary([FromBody] CustomerActivityRequestDto request)
        {
            // 1. التحقق من صحة البيانات
            if (request == null)
            {
                return BadRequest("بيانات الطلب غير صالحة.");
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                // 2. استدعاء الدالة الجديدة في السيرفس
                var result = await _reportsService.GetCustomerActivitySummaryAsync(request);

                // 3. إرجاع القائمة الجاهزة
                return Ok(result);
            }
            catch (Exception ex)
            {
                // تسجيل الخطأ
                _logger.LogError(ex, "حدث خطأ أثناء جلب تقرير حركة العملاء للفرع: {BranchId}", request.BranchId);

                // إرجاع رسالة خطأ
                return StatusCode(500, new { Message = "حدث خطأ داخلي أثناء معالجة تقرير العملاء.", Error = ex.Message });
            }
        }

        [HttpPost("cost-center")]
        public async Task<IActionResult> GetCostCenterTransactions([FromBody] CostCenterReportRequestDto request)
        {
            if (request == null)
            {
                return BadRequest("Invalid request data.");
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var result = await _reportsService.GetCostCenterTransactionsAsync(request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching cost center transactions for CostCenterNo: {CostCenterNo}", request.CostCenterNo);
                return StatusCode(500, new { Message = "Internal Server Error", Error = ex.Message });
            }
        }

        // =====================================================================
        // تقرير حركة المبيعات الجديد - مستقل تماماً
        // =====================================================================
        [HttpPost("sales-movement")]
        public async Task<IActionResult> GetSalesMovement([FromBody] SalesMovementRequestDto request)
        {
            if (request == null || !ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var result = await _reportsService.GetSalesMovementAsync(request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching sales movement for Branch: {BranchId}", request.BranchId);
                return StatusCode(500, new { Message = "حدث خطأ داخلي أثناء معالجة تقرير حركة المبيعات.", Error = ex.Message });
            }
        }

        // =====================================================================
        // تقرير حركة الموردين - مستقل تماماً
        // =====================================================================
        [HttpPost("supplier-activity")]
        public async Task<IActionResult> GetSupplierActivity([FromBody] SupplierMovementRequestDto request)
        {
            if (request == null || !ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var result = await _reportsService.GetSupplierMovementAsync(request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching supplier activity for Branch: {BranchId}", request.BranchId);
                return StatusCode(500, new { Message = "حدث خطأ داخلي أثناء معالجة تقرير حركة الموردين.", Error = ex.Message });
            }
        }

        // =====================================================================
        // تقرير أرصدة الحسابات — مستقل تماماً
        // =====================================================================
        [HttpPost("account-balances")]
        public async Task<IActionResult> GetAccountBalances([FromBody] AccountBalancesRequestDto request)
        {
            if (request == null || !ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var result = await _accountBalancesService.GetAccountBalancesAsync(request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching account balances for Branch: {BranchId}, Mode: {Mode}", request.BranchId, request.GroupingMode);
                return StatusCode(500, new { Message = "حدث خطأ داخلي أثناء معالجة تقرير أرصدة الحسابات.", Error = ex.Message });
            }
        }

        // =====================================================================
        // تقرير هامش أرباح مراكز التكلفة — مستقل تماماً
        // =====================================================================
        [HttpPost("cost-center-profit-margin")]
        public async Task<IActionResult> GetCostCenterProfitMargin(
            [FromBody] CostCenterProfitMarginRequestDto request)
        {
            if (request == null || !ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var result = await _costCenterProfitMarginService
                    .GetCostCenterProfitMarginAsync(request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error fetching cost center profit margin for Branch: {BranchId}",
                    request.BranchId);
                return StatusCode(500, new
                {
                    Message = "حدث خطأ داخلي أثناء معالجة تقرير هامش أرباح مراكز التكلفة.",
                    Error = ex.Message
                });
            }
        }
    }
}