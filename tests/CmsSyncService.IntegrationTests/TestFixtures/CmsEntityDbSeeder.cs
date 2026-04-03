using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CmsSyncService.Domain;
using CmsSyncService.Infrastructure.Persistence;

namespace CmsSyncService.Api.Tests.TestFixtures
{
    public static class CmsEntityDbSeeder
    {
        public static List<CmsEntity> SeedEntities = new();

        public static async Task ClearAsync(CmsSyncDbContext dbContext)
        {
            dbContext.CmsEntities.RemoveRange(dbContext.CmsEntities);
            await dbContext.SaveChangesAsync();
        }

        public static async Task SeedAsync(CmsSyncDbContext dbContext)
        {
            var event1 = new CmsSyncService.Domain.CmsEvent
            {
                Id = Guid.NewGuid().ToString(),
                Payload = "{\"foo\":\"bar1\"}",
                Version = 1,
                Timestamp = DateTimeOffset.UtcNow
            };
            var entity1 = CmsSyncService.Domain.CmsEntity.CreatePublished(event1);

            var event2 = new CmsSyncService.Domain.CmsEvent
            {
                Id = Guid.NewGuid().ToString(),
                Payload = "{\"foo\":\"bar2\"}",
                Version = 2,
                Timestamp = DateTimeOffset.UtcNow
            };
            var entity2 = CmsSyncService.Domain.CmsEntity.CreatePublished(event2);

            SeedEntities.Clear();
            SeedEntities.Add(entity1);
            SeedEntities.Add(entity2);

            dbContext.CmsEntities.AddRange(entity1, entity2);
            await dbContext.SaveChangesAsync();
        }
    }
}
