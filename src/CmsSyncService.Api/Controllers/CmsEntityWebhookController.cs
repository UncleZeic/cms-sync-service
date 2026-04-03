using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using CmsSyncService.Application.DTOs;
using CmsSyncService.Application.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using CmsSyncService.Application.Services;

namespace CmsSyncService.Api.Controllers
{
    [ApiController]
    [Route("cms/entities")]
    [Authorize(Roles = "Admin,EntityViewer")]
    public class CmsEntityWebhookController : ControllerBase
    {
        private readonly ICmsEntityRepository _repository;
        private readonly ILogger<CmsEntityWebhookController> _logger;
        private readonly ICmsEntityAdminService _adminService;

        public CmsEntityWebhookController(ICmsEntityRepository repository, ILogger<CmsEntityWebhookController> logger, ICmsEntityAdminService adminService)
        {
            _repository = repository;
            _logger = logger;
            _adminService = adminService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<ICmsEntityDto>>> GetAll(CancellationToken cancellationToken)
        {
            try
            {
                var entities = await _repository.GetAllAsync(cancellationToken);
                IEnumerable<ICmsEntityDto> dtos;
                if (User.IsInRole("Admin"))
                {
                    dtos = entities.Select(e => e.ToAdminDto()).ToList();
                }
                else
                {
                    dtos = entities.Where(e => e.Published).Select(e => e.ToDto()).ToList();
                }
                return Ok(dtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetAll");
                throw;
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ICmsEntityDto>> GetById(string id, CancellationToken cancellationToken)
        {
            try
            {
                var entity = await _repository.GetByIdAsync(id, cancellationToken);
                if (entity == null)
                    return NotFound();
                ICmsEntityDto dto;
                if (User.IsInRole("Admin"))
                {
                    dto = entity.ToAdminDto();
                }
                else
                {
                    dto = entity.ToDto();
                }
                return Ok(dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in GetById for id {id}");
                throw;
            }
        }

        [HttpPatch("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> PatchById(string id, [FromBody] AdminDisabledDto dto, CancellationToken cancellationToken)
        {
            try
            {
                var result = await _adminService.SetAdminDisabledAsync(id, dto.AdminDisabled, cancellationToken);
                if (!result)
                    return NotFound();
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in PatchById for id {id}");
                throw;
            }
        }
    }
}