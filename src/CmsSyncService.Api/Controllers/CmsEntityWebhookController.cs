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
        public async Task<ActionResult<PagedResponseDto<ICmsEntityDto>>> GetAll([FromQuery] int skip = 0, [FromQuery] int take = 100, CancellationToken cancellationToken = default)
        {
            try
            {
                var isAdmin = User.IsInRole("Admin");

                async Task<PagedResponseDto<ICmsEntityDto>> FetchPage()
                {
                    if (isAdmin)
                    {
                        var entities = await _repository.GetAllAsync(skip, take, cancellationToken);
                        var total = await _repository.CountAllAsync(cancellationToken);
                        return new PagedResponseDto<ICmsEntityDto>
                        {
                            Items = entities.Select(e => e.ToAdminDto()).Cast<ICmsEntityDto>().ToList(),
                            Total = total,
                            Skip = skip,
                            Take = take
                        };
                    }
                    else
                    {
                        var entities = await _repository.GetVisibleToNormalUserAsync(skip, take, cancellationToken);
                        var total = await _repository.CountVisibleToNormalUserAsync(cancellationToken);
                        return new PagedResponseDto<ICmsEntityDto>
                        {
                            Items = entities.Select(e => e.ToDto()).Cast<ICmsEntityDto>().ToList(),
                            Total = total,
                            Skip = skip,
                            Take = take
                        };
                    }
                }

                // Only cache the most common first page (skip=0, take=100)
                if (skip == EntityCacheKeys.DefaultSkip && take == EntityCacheKeys.DefaultTake)
                {
                    string cacheKey = EntityCacheKeys.GetDefaultPagedEntityListKey(isAdmin);
                    var cached = isAdmin
                        ? ToInterfacePage(_cacheService.Get<PagedResponseDto<CmsEntityAdminDto>>(cacheKey))
                        : ToInterfacePage(_cacheService.Get<PagedResponseDto<CmsEntityDto>>(cacheKey));
                    if (cached != null)
                        return Ok(cached);
                    var page = await FetchPage();
                    if (isAdmin)
                    {
                        _cacheService.Set(cacheKey, ToConcretePage<CmsEntityAdminDto>(page));
                    }
                    else
                    {
                        _cacheService.Set(cacheKey, ToConcretePage<CmsEntityDto>(page));
                    }
                    return Ok(page);
                }
                else
                {
                    var page = await FetchPage();
                    return Ok(page);
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
                var isAdmin = User.IsInRole("Admin");
                string cacheKey = EntityCacheKeys.GetEntityKey(id, isAdmin);
                ICmsEntityDto? dto = isAdmin
                    ? _cacheService.Get<CmsEntityAdminDto>(cacheKey)
                    : _cacheService.Get<CmsEntityDto>(cacheKey);
                if (dto == null)
                {
                    var entity = await _repository.GetByIdVisibleToUserAsync(id, isAdmin, cancellationToken);
                    if (entity == null)
                        return NotFound();
                    dto = isAdmin ? entity.ToAdminDto() : entity.ToDto();
                    if (isAdmin)
                    {
                        _cacheService.Set(cacheKey, (CmsEntityAdminDto)dto);
                    }
                    else
                    {
                        _cacheService.Set(cacheKey, (CmsEntityDto)dto);
                    }
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

        private static PagedResponseDto<ICmsEntityDto>? ToInterfacePage<T>(PagedResponseDto<T>? page)
            where T : ICmsEntityDto
        {
            return page is null
                ? null
                : new PagedResponseDto<ICmsEntityDto>
                {
                    Items = page.Items.Cast<ICmsEntityDto>().ToList(),
                    Total = page.Total,
                    Skip = page.Skip,
                    Take = page.Take
                };
        }

        private static PagedResponseDto<T> ToConcretePage<T>(PagedResponseDto<ICmsEntityDto> page)
            where T : ICmsEntityDto
        {
            return new PagedResponseDto<T>
            {
                Items = page.Items.Cast<T>().ToList(),
                Total = page.Total,
                Skip = page.Skip,
                Take = page.Take
            };
        }
    }
}
