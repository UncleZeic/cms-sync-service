using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using System.Text.Json;
using CmsSyncService.Domain;

namespace CmsSyncService.Application.DTOs;

public sealed class CmsEventDto : IValidatableObject
{
    private static readonly Regex SafeIdPattern = new("^[a-zA-Z0-9\\-_]+$", RegexOptions.Compiled);

    [Required]
    [MaxLength(20)]
    public string Type { get; init; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string Id { get; init; } = string.Empty;

    public JsonElement? Payload { get; init; }

    public int? Version { get; init; }

    [Required]
    public DateTimeOffset Timestamp { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var normalizedType = Type?.Trim().ToLowerInvariant();

        if (string.IsNullOrWhiteSpace(normalizedType))
        {
            yield return new ValidationResult(
                "Event type is required.",
                [nameof(Type)]);
            yield break;
        }

        if (normalizedType is not ("publish" or "unpublish" or "delete"))
        {
            yield return new ValidationResult(
                $"Unsupported event type '{Type}'.",
                [nameof(Type)]);
            yield break;
        }

        if (string.IsNullOrWhiteSpace(Id))
        {
            yield return new ValidationResult(
                "Event id is required.",
                [nameof(Id)]);
            yield break;
        }

        if (!SafeIdPattern.IsMatch(Id.Trim()))
        {
            yield return new ValidationResult(
                "Event id may only contain letters, numbers, hyphens, and underscores.",
                [nameof(Id)]);
        }

        if (Timestamp == default)
        {
            yield return new ValidationResult(
                "Timestamp must be a valid non-default value.",
                [nameof(Timestamp)]);
        }

        if (normalizedType is "publish" or "unpublish")
        {
            if (Version is null)
            {
                yield return new ValidationResult(
                    "Version is required for publish and unpublish events.",
                    [nameof(Version)]);
            }
            else if (Version <= 0)
            {
                yield return new ValidationResult(
                    "Version must be greater than zero.",
                    [nameof(Version)]);
            }

            if (Payload is null ||
                Payload.Value.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
            {
                yield return new ValidationResult(
                    "Payload is required for publish and unpublish events.",
                    [nameof(Payload)]);
            }
            else if (Payload.Value.ValueKind is not (JsonValueKind.Object or JsonValueKind.Array))
            {
                yield return new ValidationResult(
                    "Payload must be a JSON object or array.",
                    [nameof(Payload)]);
            }
        }

        if (normalizedType == "delete" &&
            Version is not null &&
            Version <= 0)
        {
            yield return new ValidationResult(
                "If version is supplied for delete, it must be greater than zero.",
                [nameof(Version)]);
        }
    }

    public CmsEvent ToNormalized()
    {
        return new CmsEvent
        {
            Type = ParseType(Type),
            Id = Id.Trim(),
            Payload = NormalizePayload(Payload),
            Version = Version,
            Timestamp = Timestamp
        };
    }

    private static CmsEventType ParseType(string rawType)
    {
        var normalized = rawType.Trim().ToLowerInvariant();

        return normalized switch
        {
            "publish" => CmsEventType.Publish,
            "unpublish" => CmsEventType.Unpublish,
            "delete" => CmsEventType.Delete,
            _ => throw new InvalidOperationException(
                $"Cannot normalize unsupported event type '{rawType}'.")
        };
    }

    public CmsEvent ToDomain()
    {
        return ToNormalized();
    }

    private static string? NormalizePayload(JsonElement? payload)
    {
        if (payload is null)
        {
            return null;
        }

        using var document = JsonDocument.Parse(payload.Value.GetRawText());
        return document.RootElement.GetRawText();
    }
}
