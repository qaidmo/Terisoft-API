using AccountingSyncAPP.Data.Dtos;
using AccountingSyncAPP.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace AccountingSyncAPP.Controllers
{
    /// <summary>
    /// تحكم تقرير هامش الربح — مستقل تماماً عن ReportsController
    /// يستقبل طلبات HTTP POST ويفوضها لـ ProfitMarginService
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class ProfitMarginController : ControllerBase
    {
        private readonly ProfitMarginService _service;
        private readonly ILogger<ProfitMarginController> _logger;

        public ProfitMarginController(ProfitMarginService service,
                                      ILogger<ProfitMarginController> logger)
        {
            _service = service;
            _logger  = logger;
        }

        /// <summary>
        /// POST /api/ProfitMargin/get-report — تقرير هامش الربح
        /// </summary>
        [HttpPost("get-report")]
        public async Task<IActionResult> GetProfitMargin([FromBody] ProfitMarginRequestDto request)
        {
            if (request == null)
                return BadRequest("بيانات الطلب غير صالحة.");

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var result = await _service.GetProfitMarginAsync(request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "خطأ في تقرير هامش الربح للفرع: {BranchId}, السنة: {Year}",
                    request.BranchId, request.Year);

                return StatusCode(500, new
                {
                    Message = "حدث خطأ داخلي أثناء معالجة تقرير هامش الربح.",
                    Error   = ex.Message
                });
            }
        }
    }
}
