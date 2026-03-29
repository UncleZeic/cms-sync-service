using Microsoft.AspNetCore.Mvc;

namespace CmsSyncService.Api.Controllers
{
    [ApiController]
    [Route("cms/events")]
    public class CmsEventWebhookController : ControllerBase
    {
        [HttpPost("")]
        public IActionResult IngestEvents()
        {
            throw new NotImplementedException("Webhook endpoint not implemented yet.");
        }
    }
}
