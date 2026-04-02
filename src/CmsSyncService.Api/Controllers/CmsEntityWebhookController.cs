
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using CmsSyncService.Application.DTOs;
using CmsSyncService.Application.Repositories;
using Microsoft.AspNetCore.Authorization;

namespace CmsSyncService.Api.Controllers
{
    [ApiController]
    [Route("cms/entities")]
    public class CmsEntityWebhookController : ControllerBase
    {
        private readonly ICmsEntityRepository _repository;

        public CmsEntityWebhookController(ICmsEntityRepository repository)
        {
            _repository = repository;
        }


        [HttpGet]
        public async Task<ActionResult<IEnumerable<CmsEntityDto>>> GetAll(CancellationToken cancellationToken)
        {
            var entities = await _repository.GetAllAsync(cancellationToken);
            return entities.Select(e => e.ToDto()).ToList();
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<CmsEntityDto>> GetById(string id, CancellationToken cancellationToken)
        {
            var entity = await _repository.GetByIdAsync(id, cancellationToken);
            if (entity == null)
                return NotFound();
            return entity.ToDto();
        }
    }
}