using System;
using CmsSyncService.Domain;
using Xunit;

namespace CmsSyncService.UnitTests.Domain;

public class CmsEntityTests
{
    [Fact]
    public void CreatePublished_SetsPropertiesCorrectly()
    {
        var cmsEvent = new CmsEvent
        {
            Id = "entity-1",
            Payload = "{\"title\":\"test\"}",
            Version = 2,
            Timestamp = DateTimeOffset.UtcNow
        };

        var entity = CmsEntity.CreatePublished(cmsEvent);

        Assert.Equal("entity-1", entity.Id);
        Assert.Equal("{\"title\":\"test\"}", entity.Payload);
        Assert.Equal(2, entity.Version);
        Assert.True(entity.Published);
        Assert.False(entity.AdminDisabled);
        Assert.Equal(cmsEvent.Timestamp, entity.UpdatedAtUtc);
    }

    [Fact]
    public void CreateUnpublished_SetsPropertiesCorrectly()
    {
        var cmsEvent = new CmsEvent
        {
            Id = "entity-2",
            Payload = "{\"title\":\"unpub\"}",
            Version = 3,
            Timestamp = DateTimeOffset.UtcNow
        };

        var entity = CmsEntity.CreateUnpublished(cmsEvent);

        Assert.Equal("entity-2", entity.Id);
        Assert.Equal("{\"title\":\"unpub\"}", entity.Payload);
        Assert.Equal(3, entity.Version);
        Assert.False(entity.Published);
        Assert.False(entity.AdminDisabled);
        Assert.Equal(cmsEvent.Timestamp, entity.UpdatedAtUtc);
    }

    [Fact]
    public void ApplyPublish_UpdatesPropertiesIfVersionIsHigher()
    {
        var initialEvent = new CmsEvent
        {
            Id = "entity-3",
            Payload = "{\"title\":\"old\"}",
            Version = 1,
            Timestamp = DateTimeOffset.UtcNow.AddDays(-1)
        };
        var entity = CmsEntity.CreatePublished(initialEvent);

        var newEvent = new CmsEvent
        {
            Id = "entity-3",
            Payload = "{\"title\":\"new\"}",
            Version = 2,
            Timestamp = DateTimeOffset.UtcNow
        };

        entity.ApplyPublish(newEvent);

        Assert.Equal("{\"title\":\"new\"}", entity.Payload);
        Assert.Equal(2, entity.Version);
        Assert.True(entity.Published);
        Assert.Equal(newEvent.Timestamp, entity.UpdatedAtUtc);
    }

    [Fact]
    public void ApplyUnpublish_UpdatesPropertiesIfVersionIsHigher()
    {
        var initialEvent = new CmsEvent
        {
            Id = "entity-4",
            Payload = "{\"title\":\"old\"}",
            Version = 1,
            Timestamp = DateTimeOffset.UtcNow.AddDays(-1)
        };
        var entity = CmsEntity.CreatePublished(initialEvent);

        var newEvent = new CmsEvent
        {
            Id = "entity-4",
            Payload = "{\"title\":\"new\"}",
            Version = 2,
            Timestamp = DateTimeOffset.UtcNow
        };

        entity.ApplyUnpublish(newEvent);

        Assert.Equal("{\"title\":\"new\"}", entity.Payload);
        Assert.Equal(2, entity.Version);
        Assert.False(entity.Published);
        Assert.Equal(newEvent.Timestamp, entity.UpdatedAtUtc);
    }

    [Fact]
    public void SetAdminDisabled_UpdatesFlagAndTimestamp()
    {
        var entity = CmsEntity.CreatePublished(new CmsEvent
        {
            Id = "entity-5",
            Payload = "{}",
            Version = 1,
            Timestamp = DateTimeOffset.UtcNow
        });
        var ts = DateTimeOffset.UtcNow.AddMinutes(5);
        entity.SetAdminDisabled(true, ts);
        Assert.True(entity.AdminDisabled);
        Assert.Equal(ts, entity.UpdatedAtUtc);
    }

    [Fact]
    public void IsVisibleToNormalUser_ReturnsTrueOnlyIfPublishedAndNotAdminDisabled()
    {
        var entity = CmsEntity.CreatePublished(new CmsEvent
        {
            Id = "entity-6",
            Payload = "{}",
            Version = 1,
            Timestamp = DateTimeOffset.UtcNow
        });
        Assert.True(entity.IsVisibleToNormalUser());
        entity.SetAdminDisabled(true, DateTimeOffset.UtcNow);
        Assert.False(entity.IsVisibleToNormalUser());
        entity.SetAdminDisabled(false, DateTimeOffset.UtcNow);
        entity.ApplyUnpublish(new CmsEvent
        {
            Id = "entity-6",
            Payload = "{}",
            Version = 2,
            Timestamp = DateTimeOffset.UtcNow
        });
        Assert.False(entity.IsVisibleToNormalUser());
    }
}
