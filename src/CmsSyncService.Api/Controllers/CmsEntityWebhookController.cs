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
using CmsSyncService.Application.Caching;
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
        private readonly IEntityCacheService _cacheService;

        public CmsEntityWebhookController(ICmsEntityRepository repository, ILogger<CmsEntityWebhookController> logger, ICmsEntityAdminService adminService, IEntityCacheService cacheService)
        {
            _repository = repository;
            _logger = logger;
            _adminService = adminService;
            _cacheService = cacheService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<ICmsEntityDto>>> GetAll([FromQuery] int skip = 0, [FromQuery] int take = 100, CancellationToken cancellationToken = default)
        {
            try
            {
                var isAdmin = User.IsInRole("Admin");

                async Task<List<ICmsEntityDto>> FetchDtos()
                {
                    if (isAdmin)
                    {
                        var entities = await _repository.GetAllAsync(skip, take, cancellationToken, true);
                        return entities.Select(e => e.ToAdminDto()).Cast<ICmsEntityDto>().ToList();
                    }
                    else
                    {
                        var entities = await _repository.GetVisibleToNormalUserAsync(skip, take, cancellationToken, true);
                        return entities.Select(e => e.ToDto()).Cast<ICmsEntityDto>().ToList();
                    }
                }

                // Only cache the most common first page (skip=0, take=100)
                if (skip == EntityCacheKeys.DefaultSkip && take == EntityCacheKeys.DefaultTake)
                {
                    string cacheKey = EntityCacheKeys.GetDefaultPagedEntityListKey(isAdmin);
                    var cached = _cacheService.Get<List<ICmsEntityDto>>(cacheKey);
                    if (cached != null)
                        return Ok(cached);
                    var dtos = await FetchDtos();
                    _cacheService.Set(cacheKey, dtos);
                    return Ok(dtos);
                }
                else
                {
                    var dtos = await FetchDtos();
                    return Ok(dtos);
                }
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
                string cacheKey = EntityCacheKeys.GetEntityKey(id, User.IsInRole("Admin"));
                var dto = _cacheService.Get<ICmsEntityDto>(cacheKey);
                if (dto == null)
                {
                    var isAdmin = User.IsInRole("Admin");
                    var entity = await _repository.GetByIdVisibleToUserAsync(id, isAdmin, cancellationToken, true);
                    if (entity == null)
                        return NotFound();
                    dto = isAdmin ? entity.ToAdminDto() : entity.ToDto();
                    _cacheService.Set(cacheKey, dto);
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
                _cacheService.Remove(EntityCacheKeys.GetDefaultPagedEntityListKey(true));
                _cacheService.Remove(EntityCacheKeys.GetDefaultPagedEntityListKey(false));
                // Also invalidate this entity's cache
                _cacheService.Remove(EntityCacheKeys.GetEntityKey(id, true));
                _cacheService.Remove(EntityCacheKeys.GetEntityKey(id, false));
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
