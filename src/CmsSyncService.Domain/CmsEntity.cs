using System;

namespace CmsSyncService.Domain
{
    public class CmsEntity
    {
        public Guid Id { get; set; }
        public uint Version { get; set; }

        public DateTime UpdatedAt { get; set; }

        public bool Published { get; set; }

        public bool AdminDisabled { get; set; }

        protected CmsEntity()
        {
            Id = Guid.NewGuid();
            UpdatedAt = DateTime.UtcNow;
        }
    }
}

