using System.Threading;
using System.Threading.Tasks;
using CmsSyncService.Application.Repositories;
using CmsSyncService.Application.Services;
using CmsSyncService.Domain;
using Moq;
using Xunit;

namespace CmsSyncService.UnitTests.Application.Services;

public class CmsEntityAdminServiceTests
{
    [Fact]
    public async Task SetAdminDisabledAsync_EntityNotFound_ReturnsFalse()
    {
        var repoMock = new Mock<ICmsEntityRepository>();
        repoMock.Setup(r => r.GetByIdAsync("id1", It.IsAny<CancellationToken>())).ReturnsAsync((CmsEntity)null!);
        var service = new CmsEntityAdminService(repoMock.Object);
        var result = await service.SetAdminDisabledAsync("id1", true, CancellationToken.None);
        Assert.False(result);
    }

    [Fact]
    public async Task SetAdminDisabledAsync_AlreadySet_ReturnsTrueWithoutSave()
    {
        var entity = CmsEntity.CreatePublished(new CmsEvent { Id = "id2", Payload = "payload", Version = 1, Timestamp = System.DateTimeOffset.UtcNow, Type = CmsEventType.Publish });
        entity.SetAdminDisabled(true);
        var repoMock = new Mock<ICmsEntityRepository>();
        repoMock.Setup(r => r.GetByIdAsync("id2", It.IsAny<CancellationToken>())).ReturnsAsync(entity);
        var service = new CmsEntityAdminService(repoMock.Object);
        var result = await service.SetAdminDisabledAsync("id2", true, CancellationToken.None);
        Assert.True(result);
        repoMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SetAdminDisabledAsync_UpdatesFlagAndSaves()
    {
        var entity = CmsEntity.CreatePublished(new CmsEvent { Id = "id3", Payload = "payload", Version = 1, Timestamp = System.DateTimeOffset.UtcNow, Type = CmsEventType.Publish });
        entity.SetAdminDisabled(false);
        var repoMock = new Mock<ICmsEntityRepository>();
        repoMock.Setup(r => r.GetByIdAsync("id3", It.IsAny<CancellationToken>())).ReturnsAsync(entity);
        var service = new CmsEntityAdminService(repoMock.Object);
        var result = await service.SetAdminDisabledAsync("id3", true, CancellationToken.None);
        Assert.True(result);
        Assert.True(entity.AdminDisabled);
        repoMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
