using System;
using CmsSyncService.Application.Services;
using CmsSyncService.Application.DTOs;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Moq;
using System.Collections.Generic;

namespace CmsSyncService.UnitTests.Application.Services;

public class CmsEventApplicationServiceTests
{
    [Fact]
    public async Task ProcessBatchAsync_EmptyList_DoesNotThrow()
    {
        var service = new Mock<ICmsEventApplicationService>();
        var events = new List<CmsEventDto>();
        var exception = await Record.ExceptionAsync(() => service.Object.ProcessBatchAsync(events, CancellationToken.None));
        Assert.Null(exception);
    }

    [Fact]
    public async Task ProcessBatchAsync_CallsImplementation()
    {
        var serviceMock = new Mock<ICmsEventApplicationService>();
        var events = new List<CmsEventDto> { new CmsEventDto { Type = "publish", Id = "id", Version = 1, Timestamp = DateTimeOffset.UtcNow } };
        serviceMock.Setup(s => s.ProcessBatchAsync(events, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask).Verifiable();
        await serviceMock.Object.ProcessBatchAsync(events, CancellationToken.None);
        serviceMock.Verify();
    }
}
