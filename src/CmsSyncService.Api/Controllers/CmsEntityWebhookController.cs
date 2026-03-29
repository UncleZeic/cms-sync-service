using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using CmsSyncService.Domain;
using Microsoft.AspNetCore.Authorization;

namespace CmsSyncService.Api.Controllers
{
    [ApiController]
    [Route("cms/entities")]
    public class CmsEntityWebhookController : ControllerBase
    {
        [HttpGet]
        public ActionResult<IEnumerable<CmsEntity>> GetAll()
        {
            throw new NotImplementedException("GetAll is not implemented yet.");
        }

        [HttpGet("{id:guid}")]
        public ActionResult<CmsEntity> GetById(Guid id)
        {
            throw new NotImplementedException("GetById is not implemented yet.");
        }
    }
}