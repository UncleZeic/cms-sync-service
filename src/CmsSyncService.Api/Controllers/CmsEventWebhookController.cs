
using CmsSyncService.Application.DTOs;
using CmsSyncService.Api.Messaging;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace CmsSyncService.Api.Controllers
{
    [ApiController]
    [Route("cms/events")]
    [Authorize(Roles = "EventUpdater")]
    [EnableRateLimiting("CmsEventIngestion")]
    public class CmsEventWebhookController : ControllerBase
    {
        private readonly ICmsEventPublisher _publisher;
        private readonly ILogger<CmsEventWebhookController> _logger;

        public CmsEventWebhookController(ICmsEventPublisher publisher, ILogger<CmsEventWebhookController> logger)
        {
            _publisher = publisher;
            _logger = logger;
        }

        [HttpPost("")]
        [RequestSizeLimit(1048576)] // 1 MB limit; adjust as needed
        public async Task<IActionResult> IngestEvents([FromBody] IEnumerable<CmsEventDto> events, CancellationToken cancellationToken)
        {
            try
            {
                if (events == null)
                    return BadRequest("No events provided.");

                var batch = events as IReadOnlyCollection<CmsEventDto> ?? events.ToList();
                if (batch.Count == 0)
                    return BadRequest("No events provided.");

                await _publisher.PublishBatchAsync(batch, cancellationToken);
                return _publisher.PublishesAsynchronously ? Accepted() : Ok();
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, "Error in IngestEvents");
                throw;
            }
        }
    }
}
