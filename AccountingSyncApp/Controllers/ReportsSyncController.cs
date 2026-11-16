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
    public class ReportsSyncController : ControllerBase
    {
        private readonly IReportsService _reportsService;
        private readonly ILogger<ReportsSyncController> _logger;

        public ReportsSyncController(IReportsService reportsService, ILogger<ReportsSyncController> logger)
        {
            _reportsService = reportsService;
            _logger = logger;
        }

        [HttpPost("delta-account-statement")]
        public async Task<IActionResult> GetDeltaAccountStatement([FromBody] AccountStatementDeltaRequestDto request)
        {
            if (request == null || !ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var result = await _reportsService.GetDeltaAccountStatementAsync(request);
                
                if (result == null)
                {
                    return StatusCode(304); // 304 Not Modified
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking delta account statement for Account: {AccountId}", request.AccountId);
                return StatusCode(500, new { Message = "Internal server error during delta sync.", Error = ex.Message });
            }
        }
    }
}
