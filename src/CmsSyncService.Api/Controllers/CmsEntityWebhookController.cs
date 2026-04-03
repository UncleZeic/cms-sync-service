
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
    [Authorize(Roles = "Admin,EntityViewer")]
    public class CmsEntityWebhookController : ControllerBase
    {
        private readonly ICmsEntityRepository _repository;
        private readonly ILogger<CmsEntityWebhookController> _logger;

        public CmsEntityWebhookController(ICmsEntityRepository repository, ILogger<CmsEntityWebhookController> logger)
        {
            _repository = repository;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<CmsEntityDto>>> GetAll(CancellationToken cancellationToken)
        {
            try
            {
                var entities = await _repository.GetAllAsync(cancellationToken);
                IEnumerable<CmsEntityDto> dtos;
                if (User.IsInRole("Admin"))
                {
                    dtos = entities.Select(e => e.ToDto());
                }
                else
                {
                    dtos = entities.Where(e => e.Published).Select(e => e.ToDto());
                }
                return Ok(dtos.ToList());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetAll");
                throw;
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<CmsEntityDto>> GetById(string id, CancellationToken cancellationToken)
        {
            try
            {
                var entity = await _repository.GetByIdAsync(id, cancellationToken);
                if (entity == null)
                    return NotFound();
                var dto = entity.ToDto();
                return Ok(dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in GetById for id {id}");
                throw;
            }
        }
    }
}