using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using CmsSyncService.Application.DTOs;
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
        Assert.Contains(results, r => r.ErrorMessage == "The Type field is required.");
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
    private static JsonElement ParseJson(string json)
    {
        using var document = JsonDocument.Parse(json);
        return document.RootElement.Clone();
    }
}
