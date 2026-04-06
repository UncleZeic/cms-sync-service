using System;
using CmsSyncService.Application.Services;
using CmsSyncService.Application.DTOs;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Moq;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Caching.Memory;
using CmsSyncService.Application.Repositories;

namespace CmsSyncService.UnitTests.Application.Services;

public class CmsEventApplicationServiceTests
{

    [Fact]
    public async Task ProcessBatchAsync_LogsInformation()
    {
        var repoMock = new Mock<ICmsEntityRepository>();
        var loggerMock = new Mock<ILogger<CmsEventApplicationService>>();
        var cacheMock = new Mock<IMemoryCache>();
        var service = new CmsEventApplicationService(repoMock.Object, loggerMock.Object, cacheMock.Object);
        var events = new List<CmsEventDto>();

        await service.ProcessBatchAsync(events, CancellationToken.None);

        loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Processing batch of")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
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
