using AccountingSyncAPP.Data.Dtos;
using AccountingSyncAPP.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace AccountingSyncAPP.Controllers
{
    /// <summary>
    /// تحكم تقرير بطاقة الصنف - مستقل تماماً عن ReportsController
    /// يستقبل طلبات HTTP POST ويفوضها لـ ItemCardService
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class ItemCardController : ControllerBase
    {
        private readonly ItemCardService _service;
        private readonly ILogger<ItemCardController> _logger;

        public ItemCardController(ItemCardService service, ILogger<ItemCardController> logger)
        {
            _service = service;
            _logger  = logger;
        }

        /// <summary>
        /// POST /api/ItemCard/get-report — تقرير بطاقة الصنف
        /// </summary>
        [HttpPost("get-report")]
        public async Task<IActionResult> GetItemCard([FromBody] ItemCardRequestDto request)
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
                var result = await _service.GetItemCardAsync(request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "خطأ في تقرير بطاقة الصنف للفرع: {BranchId}, السنة: {Year}",
                    request.BranchId, request.Year);

                return StatusCode(500, new
                {
                    Message = "حدث خطأ داخلي أثناء معالجة تقرير بطاقة الصنف.",
                    Error   = ex.Message
                });
            }
        }
    }
}
