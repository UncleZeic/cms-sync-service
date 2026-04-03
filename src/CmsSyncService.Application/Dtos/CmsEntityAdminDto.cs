namespace CmsSyncService.Application.DTOs;

public class CmsEntityAdminDto : CmsEntityDto, ICmsEntityDto
{
    public bool Published { get; init; }
    public bool AdminDisabled { get; init; }
}