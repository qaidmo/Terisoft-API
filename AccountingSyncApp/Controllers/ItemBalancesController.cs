using AccountingSyncAPP.Data.Dtos;
using AccountingSyncAPP.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace AccountingSyncAPP.Controllers
{
    /// <summary>
    /// تحكم تقرير أرصدة الأصناف — مستقل تماماً
    /// POST /api/ItemBalances/get-report
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class ItemBalancesController : ControllerBase
    {
        private readonly ItemBalancesService _service;
        private readonly ILogger<ItemBalancesController> _logger;

        public ItemBalancesController(ItemBalancesService service,
                                      ILogger<ItemBalancesController> logger)
        {
            _service = service;
            _logger  = logger;
        }

        /// <summary>
        /// POST /api/ItemBalances/get-report
        /// </summary>
        [HttpPost("get-report")]
        public async Task<IActionResult> GetItemBalances([FromBody] ItemBalancesRequestDto request)
        {
            if (request == null)
                return BadRequest("بيانات الطلب غير صالحة.");

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var result = await _service.GetItemBalancesAsync(request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "خطأ في تقرير أرصدة الأصناف — الفرع: {BranchId}, السنة: {Year}",
                    request.BranchId, request.Year);

                return StatusCode(500, new
                {
                    Message = "حدث خطأ داخلي أثناء معالجة تقرير أرصدة الأصناف.",
                    Error   = ex.Message
                });
            }
        }
    }
}
