using AccountingSyncAPP.Data.Dtos;
using AccountingSyncAPP.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace AccountingSyncAPP.Controllers
{
    /// <summary>
    /// تحكم تقرير حركة الأصناف - مستقل تماماً عن ReportsController
    /// يستقبل طلبات HTTP POST ويفوضها لـ ItemMovementService
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class ItemMovementController : ControllerBase
    {
        private readonly ItemMovementService _service;
        private readonly ILogger<ItemMovementController> _logger;

        public ItemMovementController(ItemMovementService service, ILogger<ItemMovementController> logger)
        {
            _service = service;
            _logger  = logger;
        }

        /// <summary>
        /// GET /api/ItemMovement/get-report — تقرير حركة الأصناف
        /// </summary>
        [HttpPost("get-report")]
        public async Task<IActionResult> GetItemMovement([FromBody] ItemMovementRequestDto request)
        {
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
                var result = await _service.GetItemMovementAsync(request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "خطأ في تقرير حركة الأصناف للفرع: {BranchId}, السنة: {Year}",
                    request.BranchId, request.Year);

                return StatusCode(500, new
                {
                    Message = "حدث خطأ داخلي أثناء معالجة تقرير حركة الأصناف.",
                    Error   = ex.Message
                });
            }
        }
    }
}
