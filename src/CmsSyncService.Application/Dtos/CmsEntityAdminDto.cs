namespace CmsSyncService.Application.DTOs;

public class CmsEntityAdminDto : CmsEntityDto
{
    public bool Published { get; init; }
    public bool AdminDisabled { get; init; }
}