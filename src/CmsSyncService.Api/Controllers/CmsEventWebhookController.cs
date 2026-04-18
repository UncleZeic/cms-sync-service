
using CmsSyncService.Application.DTOs;
using CmsSyncService.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CmsSyncService.Api.Controllers
{
    [ApiController]
    [Route("cms/events")]
    [Authorize(Roles = "EventUpdater")]
    public class CmsEventWebhookController : ControllerBase
    {
        private readonly ICmsEventApplicationService _service;
        private readonly ILogger<CmsEventWebhookController> _logger;

        public CmsEventWebhookController(ICmsEventApplicationService service, ILogger<CmsEventWebhookController> logger)
        {
            _service = service;
            _logger = logger;
        }

        [HttpPost("")]
        [RequestSizeLimit(1048576)] // 1 MB limit; adjust as needed
        public async Task<IActionResult> IngestEvents([FromBody] IEnumerable<CmsEventDto> events, CancellationToken cancellationToken)
        {
            try
            {
                if (events == null || !events.Any())
                    return BadRequest("No events provided.");

                await _service.ProcessBatchAsync(events, cancellationToken);
                return Ok();
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, "Error in IngestEvents");
                throw;
            }
        }
    }
}
