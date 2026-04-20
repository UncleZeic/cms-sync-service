using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using CmsSyncService.Application.DTOs;
using CmsSyncService.Domain;
using Xunit;

namespace CmsSyncService.UnitTests.Application.Dtos;

public class CmsEventDtoTests
{
    [Fact]
    public void Validate_ValidPublishEvent_ReturnsNoErrors()
    {
        var dto = new CmsEventDto
        {
            Type = "publish",
            Id = "entity-1",
            Version = 1,
            Payload = ParseJson("{}"),
            Timestamp = DateTimeOffset.UtcNow
        };
        var results = new List<ValidationResult>();
        var context = new ValidationContext(dto);
        bool isValid = Validator.TryValidateObject(dto, context, results, true);
        Assert.True(isValid);
        Assert.Empty(results);
    }

    [Fact]
    public void Validate_MissingType_ReturnsError()
    {
        var dto = new CmsEventDto
        {
            Id = "entity-2",
            Version = 1,
            Payload = ParseJson("{}"),
            Timestamp = DateTimeOffset.UtcNow
        };
        var results = new List<ValidationResult>();
        var context = new ValidationContext(dto);
        bool isValid = Validator.TryValidateObject(dto, context, results, true);
        Assert.False(isValid);
        Assert.Contains(results, r => r.ErrorMessage == "Event type is required.");
    }

    [Fact]
    public void Validate_InvalidType_ReturnsError()
    {
        var dto = new CmsEventDto
        {
            Type = "invalid",
            Id = "entity-3",
            Version = 1,
            Payload = ParseJson("{}"),
            Timestamp = DateTimeOffset.UtcNow
        };
        var results = new List<ValidationResult>();
        var context = new ValidationContext(dto);
        bool isValid = Validator.TryValidateObject(dto, context, results, true);
        Assert.False(isValid);
        Assert.Contains(results, r => r.ErrorMessage!.Contains("Unsupported event type"));
    }

    [Fact]
    public void Validate_PublishEvent_MissingVersion_ReturnsError()
    {
        var dto = new CmsEventDto
        {
            Type = "publish",
            Id = "entity-4",
            Payload = ParseJson("{}"),
            Timestamp = DateTimeOffset.UtcNow
        };
        var results = new List<ValidationResult>();
        var context = new ValidationContext(dto);
        bool isValid = Validator.TryValidateObject(dto, context, results, true);
        Assert.False(isValid);
        Assert.Contains(results, r => r.ErrorMessage!.Contains("Version is required for publish and unpublish events"));
    }

    [Fact]
    public void Validate_PublishEvent_MissingPayload_ReturnsError()
    {
        var dto = new CmsEventDto
        {
            Type = "publish",
            Id = "entity-5",
            Version = 1,
            Timestamp = DateTimeOffset.UtcNow
        };
        var results = new List<ValidationResult>();
        var context = new ValidationContext(dto);
        bool isValid = Validator.TryValidateObject(dto, context, results, true);
        Assert.False(isValid);
        Assert.Contains(results, r => r.ErrorMessage!.Contains("Payload is required for publish and unpublish events"));
    }

    [Fact]
    public void Validate_PublishEvent_StringPayload_ReturnsError()
    {
        var dto = new CmsEventDto
        {
            Type = "publish",
            Id = "entity-5b",
            Version = 1,
            Payload = ParseJson("\"unsafe\""),
            Timestamp = DateTimeOffset.UtcNow
        };
        var results = new List<ValidationResult>();
        var context = new ValidationContext(dto);
        bool isValid = Validator.TryValidateObject(dto, context, results, true);
        Assert.False(isValid);
        Assert.Contains(results, r => r.ErrorMessage!.Contains("Payload must be a JSON object or array"));
    }

    [Fact]
    public void Validate_DeleteEvent_InvalidVersion_ReturnsError()
    {
        var dto = new CmsEventDto
        {
            Type = "delete",
            Id = "entity-6",
            Version = 0,
            Timestamp = DateTimeOffset.UtcNow
        };
        var results = new List<ValidationResult>();
        var context = new ValidationContext(dto);
        bool isValid = Validator.TryValidateObject(dto, context, results, true);
        Assert.False(isValid);
        Assert.Contains(results, r => r.ErrorMessage!.Contains("If version is supplied for delete, it must be greater than zero"));
    }

    [Fact]
    public void Validate_WhitespaceId_ReturnsError()
    {
        var dto = new CmsEventDto
        {
            Type = "publish",
            Id = "   ",
            Version = 1,
            Payload = ParseJson("{}"),
            Timestamp = DateTimeOffset.UtcNow
        };
        var results = new List<ValidationResult>();
        var context = new ValidationContext(dto);
        bool isValid = Validator.TryValidateObject(dto, context, results, true);
        Assert.False(isValid);
        Assert.Contains(results, r => r.ErrorMessage == "Event id is required.");
    }

    [Fact]
    public void Validate_PathTraversalLikeId_ReturnsError()
    {
        var dto = new CmsEventDto
        {
            Type = "publish",
            Id = "../entity",
            Version = 1,
            Payload = ParseJson("{}"),
            Timestamp = DateTimeOffset.UtcNow
        };
        var results = new List<ValidationResult>();
        var context = new ValidationContext(dto);
        bool isValid = Validator.TryValidateObject(dto, context, results, true);
        Assert.False(isValid);
        Assert.Contains(results, r => r.ErrorMessage == "Event id may only contain letters, numbers, hyphens, and underscores.");
    }

    [Fact]
    public void Validate_DefaultTimestamp_ReturnsError()
    {
        var dto = new CmsEventDto
        {
            Type = "publish",
            Id = "entity-8",
            Version = 1,
            Payload = ParseJson("{}"),
            Timestamp = default
        };
        var results = new List<ValidationResult>();
        var context = new ValidationContext(dto);
        bool isValid = Validator.TryValidateObject(dto, context, results, true);
        Assert.False(isValid);
        Assert.Contains(results, r => r.ErrorMessage == "Timestamp must be a valid non-default value.");
    }

    [Fact]
    public void ToNormalized_TrimAndCanonicalizeValues()
    {
        var dto = new CmsEventDto
        {
            Type = " Publish ",
            Id = " entity-7 ",
            Version = 1,
            Payload = ParseJson("{  \"foo\" : \"bar\" }"),
            Timestamp = DateTimeOffset.UtcNow
        };

        var normalized = dto.ToNormalized();

        Assert.Equal(CmsEventType.Publish, normalized.Type);
        Assert.Equal("entity-7", normalized.Id);
        Assert.Equal("{\"foo\":\"bar\"}", normalized.Payload);
    }

    private static JsonElement ParseJson(string json)
    {
        using var document = JsonDocument.Parse(json);
        return document.RootElement.Clone();
    }
}
