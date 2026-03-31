using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using CmsSyncService.Application.DTOs;
using CmsSyncService.Domain;

namespace CmsSyncService.Api.Tests.Validation;

public sealed class CmsEventDtoValidationTests
{
    [Fact]
    public void Validate_ValidPublishEvent_ReturnsNoErrors()
    {
        var dto = new CmsEventDto
        {
            Type = "publish",
            Id = "entity-1",
            Version = 1,
            Payload = ParseJson("""{"title":"hello"}"""),
            Timestamp = DateTimeOffset.UtcNow
        };

        var results = Validate(dto);

        Assert.Empty(results);
    }

    [Fact]
    public void Validate_ValidUnpublishEvent_ReturnsNoErrors()
    {
        var dto = new CmsEventDto
        {
            Type = "unPublish",
            Id = "entity-2",
            Version = 2,
            Payload = ParseJson("""{"title":"draft"}"""),
            Timestamp = DateTimeOffset.UtcNow
        };

        var results = Validate(dto);

        Assert.Empty(results);
    }

    [Fact]
    public void Validate_DeleteWithoutPayloadAndVersion_ReturnsNoErrors()
    {
        var dto = new CmsEventDto
        {
            Type = "delete",
            Id = "entity-3",
            Timestamp = DateTimeOffset.UtcNow
        };

        var results = Validate(dto);

        Assert.Empty(results);
    }

    [Fact]
    public void Validate_MissingType_ReturnsError()
    {
        var dto = new CmsEventDto
        {
            Type = "",
            Id = "entity-4",
            Timestamp = DateTimeOffset.UtcNow
        };

        var results = Validate(dto);

        Assert.Contains(results, x => x.MemberNames.Contains(nameof(CmsEventDto.Type)));
    }

    [Fact]
    public void Validate_WhitespaceType_ReturnsError()
    {
        var dto = new CmsEventDto
        {
            Type = "   ",
            Id = "entity-5",
            Timestamp = DateTimeOffset.UtcNow
        };

        var results = Validate(dto);

        Assert.Contains(results, x => x.MemberNames.Contains(nameof(CmsEventDto.Type)));
    }

    [Fact]
    public void Validate_UnsupportedType_ReturnsError()
    {
        var dto = new CmsEventDto
        {
            Type = "update",
            Id = "entity-6",
            Timestamp = DateTimeOffset.UtcNow
        };

        var results = Validate(dto);

        Assert.Contains(results, x => x.MemberNames.Contains(nameof(CmsEventDto.Type)));
    }

    [Fact]
    public void Validate_PublishWithoutVersion_ReturnsError()
    {
        var dto = new CmsEventDto
        {
            Type = "publish",
            Id = "entity-7",
            Payload = ParseJson("""{"title":"hello"}"""),
            Timestamp = DateTimeOffset.UtcNow
        };

        var results = Validate(dto);

        Assert.Contains(results, x => x.MemberNames.Contains(nameof(CmsEventDto.Version)));
    }

    [Fact]
    public void Validate_UnpublishWithoutPayload_ReturnsError()
    {
        var dto = new CmsEventDto
        {
            Type = "unpublish",
            Id = "entity-8",
            Version = 3,
            Timestamp = DateTimeOffset.UtcNow
        };

        var results = Validate(dto);

        Assert.Contains(results, x => x.MemberNames.Contains(nameof(CmsEventDto.Payload)));
    }

    [Fact]
    public void Validate_PublishWithZeroVersion_ReturnsError()
    {
        var dto = new CmsEventDto
        {
            Type = "publish",
            Id = "entity-9",
            Version = 0,
            Payload = ParseJson("""{"title":"hello"}"""),
            Timestamp = DateTimeOffset.UtcNow
        };

        var results = Validate(dto);

        Assert.Contains(results, x => x.MemberNames.Contains(nameof(CmsEventDto.Version)));
    }

    [Fact]
    public void Validate_DeleteWithZeroVersion_ReturnsError()
    {
        var dto = new CmsEventDto
        {
            Type = "delete",
            Id = "entity-10",
            Version = 0,
            Timestamp = DateTimeOffset.UtcNow
        };

        var results = Validate(dto);

        Assert.Contains(results, x => x.MemberNames.Contains(nameof(CmsEventDto.Version)));
    }

    [Fact]
    public void ToNormalized_ValidDto_ReturnsNormalizedCmsEvent()
    {
        var dto = new CmsEventDto
        {
            Type = " publish ",
            Id = "  entity-11  ",
            Version = 5,
            Payload = ParseJson("""{"title":"hello"}"""),
            Timestamp = DateTimeOffset.UtcNow
        };

        var normalized = dto.ToNormalized();

        Assert.Equal(CmsEventType.Publish, normalized.Type);
        Assert.Equal("entity-11", normalized.ExternalId);
        Assert.Equal(5, normalized.Version);
    }

    private static List<ValidationResult> Validate(CmsEventDto dto)
    {
        var context = new ValidationContext(dto);
        var results = new List<ValidationResult>();

        Validator.TryValidateObject(
            dto,
            context,
            results,
            validateAllProperties: true);

        return results;
    }

    private static JsonElement ParseJson(string json)
    {
        using var document = JsonDocument.Parse(json);
        return document.RootElement.Clone();
    }
}