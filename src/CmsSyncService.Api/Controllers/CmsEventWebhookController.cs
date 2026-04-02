
using CmsSyncService.Application.DTOs;
using CmsSyncService.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace CmsSyncService.Api.Controllers
{
    [ApiController]
    [Route("cms/events")]
    public class CmsEventWebhookController : ControllerBase
    {
        private readonly ICmsEventApplicationService _service;

        public CmsEventWebhookController(ICmsEventApplicationService service)
        {
            _service = service;
        }

        [HttpPost("")]
        public async Task<IActionResult> IngestEvents([FromBody] List<CmsEventDto> events, CancellationToken cancellationToken)
        {
            if (events == null || events.Count == 0)
                return BadRequest("No events provided.");

            await _service.ProcessBatchAsync(events, cancellationToken);
            return Ok();
        }
    }
}
