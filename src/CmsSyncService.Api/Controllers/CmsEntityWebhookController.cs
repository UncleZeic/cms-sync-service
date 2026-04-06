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
using Microsoft.Extensions.Caching.Memory;
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
        private readonly IMemoryCache _cache;

        public CmsEntityWebhookController(ICmsEntityRepository repository, ILogger<CmsEntityWebhookController> logger, ICmsEntityAdminService adminService, IMemoryCache cache)
        {
            _repository = repository;
            _logger = logger;
            _adminService = adminService;
            _cache = cache;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<ICmsEntityDto>>> GetAll(CancellationToken cancellationToken)
        {
            try
            {
                string cacheKey = User.IsInRole("Admin") ? "entities_admin" : "entities_viewer";
                if (!_cache.TryGetValue(cacheKey, out var dtosObj) || dtosObj is not List<ICmsEntityDto> dtos)
                {
                    var entities = await _repository.GetAllAsync(cancellationToken);
                    if (User.IsInRole("Admin"))
                    {
                        dtos = entities.Select(e => e.ToAdminDto()).Cast<ICmsEntityDto>().ToList();
                    }
                    else
                    {
                        dtos = entities.Where(e => e.Published).Select(e => e.ToDto()).Cast<ICmsEntityDto>().ToList();
                    }
                    _cache.Set(cacheKey, dtos, TimeSpan.FromMinutes(5));
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
                string cacheKey = User.IsInRole("Admin") ? $"entity_admin_{id}" : $"entity_viewer_{id}";
                if (!_cache.TryGetValue(cacheKey, out var dtoObj) || dtoObj is not ICmsEntityDto dto)
                {
                    var entity = await _repository.GetByIdAsync(id, cancellationToken);
                    if (entity == null)
                        return NotFound();
                    if (User.IsInRole("Admin"))
                    {
                        dto = entity.ToAdminDto();
                    }
                    else
                    {
                        dto = entity.ToDto();
                    }
                    _cache.Set(cacheKey, dto, TimeSpan.FromMinutes(5));
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
                // Invalidate entity list caches for both admin and viewer
                _cache.Remove("entities_admin");
                _cache.Remove("entities_viewer");
                // Also invalidate this entity's cache
                _cache.Remove($"entity_admin_{id}");
                _cache.Remove($"entity_viewer_{id}");
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