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
using CmsSyncService.Domain;
using System.Text.Json;

namespace CmsSyncService.UnitTests.Application.Services;

public class CmsEventApplicationServiceTests
{

    [Fact]
    public async Task ProcessBatchAsync_LogsInformation()
    {
        var repoMock = new Mock<ICmsEntityRepository>();
        SetupTransaction(repoMock);

        repoMock.Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>())).ReturnsAsync(new List<CmsEntity>());
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
    public async Task ProcessBatchAsync_NullEvents_ThrowsArgumentNullException()
    {
        var repoMock = new Mock<ICmsEntityRepository>();
        var loggerMock = new Mock<ILogger<CmsEventApplicationService>>();
        var cacheMock = new Mock<IMemoryCache>();
        var service = new CmsEventApplicationService(repoMock.Object, loggerMock.Object, cacheMock.Object);
        await Assert.ThrowsAsync<ArgumentNullException>(() => service.ProcessBatchAsync(null!));
    }

    [Fact]
    public async Task ProcessBatchAsync_InvalidDto_LogsValidationWarningAndSkips()
    {
        var repoMock = new Mock<ICmsEntityRepository>();
        SetupTransaction(repoMock);
        repoMock.Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>())).ReturnsAsync(new List<CmsEntity>());
        var loggerMock = new Mock<ILogger<CmsEventApplicationService>>();
        var cacheMock = new Mock<IMemoryCache>();
        var service = new CmsEventApplicationService(repoMock.Object, loggerMock.Object, cacheMock.Object);
        var invalidDto = new CmsEventDto { Id = null!, Type = null!, Version = 1, Timestamp = DateTimeOffset.UtcNow };
        var events = new List<CmsEventDto> { invalidDto };
        await service.ProcessBatchAsync(events, CancellationToken.None);
        loggerMock.Verify(x => x.Log(
            LogLevel.Warning,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Event validation failed")),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task ProcessBatchAsync_DeleteEventForNonExistentEntity_LogsWarningAndSkips()
    {
        var repoMock = new Mock<ICmsEntityRepository>();
        SetupTransaction(repoMock);
        repoMock.Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>())).ReturnsAsync(new List<CmsEntity>());
        var loggerMock = new Mock<ILogger<CmsEventApplicationService>>();
        var cacheMock = new Mock<IMemoryCache>();
        var service = new CmsEventApplicationService(repoMock.Object, loggerMock.Object, cacheMock.Object);
        var events = new List<CmsEventDto> { new CmsEventDto { Id = "id", Type = "delete", Version = 1, Timestamp = DateTimeOffset.UtcNow } };
        await service.ProcessBatchAsync(events, CancellationToken.None);
        loggerMock.Verify(x => x.Log(
            LogLevel.Warning,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Delete event for non-existent entity")),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task ProcessBatchAsync_PublishWithLowerVersion_LogsVersionConflictAndSkips()
    {
        var repoMock = new Mock<ICmsEntityRepository>();
        SetupTransaction(repoMock);
        // Use the domain factory method to create an entity with version 2
        var eventDto = new CmsEventDto { Id = "id", Type = "publish", Version = 1, Timestamp = DateTimeOffset.UtcNow, Payload = JsonDocument.Parse("{\"foo\":\"bar\"}").RootElement };
        var existingEntity = CmsEntity.CreatePublished(new CmsEvent { Id = "id", Payload = "{\"foo\":\"bar\"}", Version = 2, Timestamp = eventDto.Timestamp, Type = CmsEventType.Publish });
        repoMock.Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>())).ReturnsAsync(new List<CmsEntity> { existingEntity });
        var loggerMock = new Mock<ILogger<CmsEventApplicationService>>();
        var cacheMock = new Mock<IMemoryCache>();
        var service = new CmsEventApplicationService(repoMock.Object, loggerMock.Object, cacheMock.Object);
        var events = new List<CmsEventDto> { new CmsEventDto { Id = "id", Type = "publish", Version = 1, Timestamp = DateTimeOffset.UtcNow, Payload = JsonDocument.Parse("{\"foo\":\"bar\"}").RootElement } };
        await service.ProcessBatchAsync(events, CancellationToken.None);
        loggerMock.Verify(x => x.Log(
            LogLevel.Warning,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Publish event version conflict")),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task ProcessBatchAsync_UnpublishForNonExistentEntity_AddsNewUnpublishedEntity()
    {
        var repoMock = new Mock<ICmsEntityRepository>();
        SetupTransaction(repoMock);
        repoMock.Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>())).ReturnsAsync(new List<CmsEntity>());
        repoMock.Setup(r => r.AddAsync(It.IsAny<CmsEntity>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask).Verifiable();
        var loggerMock = new Mock<ILogger<CmsEventApplicationService>>();
        var cacheMock = new Mock<IMemoryCache>();
        var service = new CmsEventApplicationService(repoMock.Object, loggerMock.Object, cacheMock.Object);
        var events = new List<CmsEventDto> {
            new CmsEventDto {
                Id = "id",
                Type = "unpublish",
                Version = 1,
                Timestamp = DateTimeOffset.UtcNow,
                Payload = JsonDocument.Parse("{\"foo\":\"bar\"}").RootElement
            }
        };
        await service.ProcessBatchAsync(events, CancellationToken.None);
        repoMock.Verify(r => r.AddAsync(It.IsAny<CmsEntity>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    private static void SetupTransaction(Mock<ICmsEntityRepository> repoMock)
    {
        repoMock
            .Setup(r => r.ExecuteInTransactionAsync(It.IsAny<Func<Task>>(), It.IsAny<CancellationToken>()))
            .Returns<Func<Task>, CancellationToken>((operation, _) => operation());
    }
}
