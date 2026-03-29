using System;
using System.Text.Json;

namespace CmsSyncService.Domain
{
    public class CmsEventProcessingLog
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public CmsEventType EventType { get; set; }
        public JsonDocument Payload { get; set; }
        public uint Version { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    public enum CmsEventType
    {
        Publish,
        Unpublish,
        Delete,
    }
}