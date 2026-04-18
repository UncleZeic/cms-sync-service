using System.Threading.Tasks;
using CmsSyncService.Domain;
using CmsSyncService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Xunit;

namespace CmsSyncService.UnitTests.Infrastructure.Persistence;

public class CmsEntityRepositoryAdvancedTests
{
    [Fact]
    public async Task GetByIdsAsync_ReturnsMatchingEntities()
    {
        const string databaseName = "GetByIdsAsync_ReturnsMatchingEntities";

        var repo = CreateRepository(databaseName);
        var entity1 = CmsEntity.CreatePublished(new CmsEvent { Id = "id1", Payload = "payload1", Version = 1, Timestamp = System.DateTimeOffset.UtcNow, Type = CmsEventType.Publish });
        var entity2 = CmsEntity.CreatePublished(new CmsEvent { Id = "id2", Payload = "payload2", Version = 2, Timestamp = System.DateTimeOffset.UtcNow, Type = CmsEventType.Publish });
        var entity3 = CmsEntity.CreateUnpublished(new CmsEvent { Id = "id3", Payload = "payload3", Version = 3, Timestamp = System.DateTimeOffset.UtcNow, Type = CmsEventType.Unpublish });
        await repo.AddAsync(entity1);
        await repo.AddAsync(entity2);
        await repo.AddAsync(entity3);
        await repo.SaveChangesAsync();

        var result = await repo.GetByIdsAsync(new List<string> { "id1", "id3", "notfound" });
        Assert.Equal(2, result.Count);
        Assert.Contains(result, e => e.Id == "id1");
        Assert.Contains(result, e => e.Id == "id3");
        Assert.DoesNotContain(result, e => e.Id == "id2");
    }
    [Fact]
    public async Task GetByIdVisibleToUserAsync_Admin_GetsAnyEntity()
    {
        const string databaseName = "GetByIdVisibleToUserAsync_Admin_GetsAnyEntity";

        var repo = CreateRepository(databaseName);
        var visible = CmsEntity.CreatePublished(new CmsEvent { Id = "id1", Payload = "payload1", Version = 1, Timestamp = System.DateTimeOffset.UtcNow, Type = CmsEventType.Publish });
        var adminDisabled = CmsEntity.CreatePublished(new CmsEvent { Id = "id2", Payload = "payload2", Version = 2, Timestamp = System.DateTimeOffset.UtcNow, Type = CmsEventType.Publish });
        adminDisabled.SetAdminDisabled(true);
        var unpublished = CmsEntity.CreateUnpublished(new CmsEvent { Id = "id3", Payload = "payload3", Version = 3, Timestamp = System.DateTimeOffset.UtcNow, Type = CmsEventType.Unpublish });
        await repo.AddAsync(visible);
        await repo.AddAsync(adminDisabled);
        await repo.AddAsync(unpublished);
        await repo.SaveChangesAsync();

        Assert.NotNull(await repo.GetByIdVisibleToUserAsync("id1", true));
        Assert.NotNull(await repo.GetByIdVisibleToUserAsync("id2", true));
        Assert.NotNull(await repo.GetByIdVisibleToUserAsync("id3", true));
    }

    [Fact]
    public async Task GetByIdVisibleToUserAsync_NonAdmin_GetsOnlyVisible()
    {
        const string databaseName = "GetByIdVisibleToUserAsync_NonAdmin_GetsOnlyVisible";

        var repo = CreateRepository(databaseName);
        var visible = CmsEntity.CreatePublished(new CmsEvent { Id = "id1", Payload = "payload1", Version = 1, Timestamp = System.DateTimeOffset.UtcNow, Type = CmsEventType.Publish });
        var adminDisabled = CmsEntity.CreatePublished(new CmsEvent { Id = "id2", Payload = "payload2", Version = 2, Timestamp = System.DateTimeOffset.UtcNow, Type = CmsEventType.Publish });
        adminDisabled.SetAdminDisabled(true);
        var unpublished = CmsEntity.CreateUnpublished(new CmsEvent { Id = "id3", Payload = "payload3", Version = 3, Timestamp = System.DateTimeOffset.UtcNow, Type = CmsEventType.Unpublish });
        await repo.AddAsync(visible);
        await repo.AddAsync(adminDisabled);
        await repo.AddAsync(unpublished);
        await repo.SaveChangesAsync();

        Assert.NotNull(await repo.GetByIdVisibleToUserAsync("id1", false));
        Assert.Null(await repo.GetByIdVisibleToUserAsync("id2", false));
        Assert.Null(await repo.GetByIdVisibleToUserAsync("id3", false));
    }


    private static CmsEntityRepository CreateRepository(string databaseName)
    {
        var databaseRoot = new InMemoryDatabaseRoot();
        var writeOptions = new DbContextOptionsBuilder<CmsSyncDbContext>()
            .UseInMemoryDatabase(databaseName, databaseRoot)
            .Options;
        var readOptions = new DbContextOptionsBuilder<CmsSyncReadDbContext>()
            .UseInMemoryDatabase(databaseName, databaseRoot)
            .Options;

        var writeContext = new CmsSyncDbContext(writeOptions);
        var readContext = new CmsSyncReadDbContext(readOptions);
        return new CmsEntityRepository(writeContext, readContext);
    }

    [Fact]
    public async Task UpdateEntity_WorksCorrectly()
    {
        const string databaseName = "UpdateEntity_WorksCorrectly";

        var repo = CreateRepository(databaseName);
        var entity = CmsEntity.CreatePublished(new CmsEvent { Id = "id1", Payload = "payload", Version = 1, Timestamp = System.DateTimeOffset.UtcNow, Type = CmsEventType.Publish });
        await repo.AddAsync(entity);
        await repo.SaveChangesAsync();

        // Update
        typeof(CmsEntity).GetProperty("Payload")!.SetValue(entity, "updated");
        await repo.SaveChangesAsync();

        var fetched = await repo.GetByIdAsync("id1");
        Assert.Equal("updated", fetched!.Payload);
    }

    [Fact]
    public async Task GetVisibleToNormalUserAsync_ReturnsOnlyVisibleEntities()
    {
        const string databaseName = "GetVisibleToNormalUserAsync_ReturnsOnlyVisibleEntities";

        var repo = CreateRepository(databaseName);
        var visible = CmsEntity.CreatePublished(new CmsEvent { Id = "id1", Payload = "payload1", Version = 1, Timestamp = System.DateTimeOffset.UtcNow, Type = CmsEventType.Publish });
        var adminDisabled = CmsEntity.CreatePublished(new CmsEvent { Id = "id2", Payload = "payload2", Version = 2, Timestamp = System.DateTimeOffset.UtcNow, Type = CmsEventType.Publish });
        adminDisabled.SetAdminDisabled(true);
        var unpublished = CmsEntity.CreateUnpublished(new CmsEvent { Id = "id3", Payload = "payload3", Version = 3, Timestamp = System.DateTimeOffset.UtcNow, Type = CmsEventType.Unpublish });
        await repo.AddAsync(visible);
        await repo.AddAsync(adminDisabled);
        await repo.AddAsync(unpublished);
        await repo.SaveChangesAsync();

        var result = await repo.GetVisibleToNormalUserAsync();
        Assert.Single(result);
        Assert.Equal("id1", result[0].Id);
    }
    
    [Fact]
    public async Task AddDuplicateId_ThrowsOrOverwrites()
    {
        const string databaseName = "AddDuplicateId_ThrowsOrOverwrites";

        var repo = CreateRepository(databaseName);
        var entity1 = CmsEntity.CreatePublished(new CmsEvent { Id = "id1", Payload = "payload1", Version = 1, Timestamp = System.DateTimeOffset.UtcNow, Type = CmsEventType.Publish });
        var entity2 = CmsEntity.CreatePublished(new CmsEvent { Id = "id1", Payload = "payload2", Version = 2, Timestamp = System.DateTimeOffset.UtcNow, Type = CmsEventType.Publish });
        await repo.AddAsync(entity1);
        await repo.SaveChangesAsync();
        await Assert.ThrowsAsync<InvalidOperationException>(async () => {
            await repo.AddAsync(entity2);
            await repo.SaveChangesAsync();
        });
    }

    [Fact]
    public async Task Query_FilterEntities()
    {
        const string databaseName = "Query_FilterEntities";
        var databaseRoot = new InMemoryDatabaseRoot();
        var writeOptions = new DbContextOptionsBuilder<CmsSyncDbContext>()
            .UseInMemoryDatabase(databaseName, databaseRoot)
            .Options;
        var readOptions = new DbContextOptionsBuilder<CmsSyncReadDbContext>()
            .UseInMemoryDatabase(databaseName, databaseRoot)
            .Options;

        var repo = new CmsEntityRepository(new CmsSyncDbContext(writeOptions), new CmsSyncReadDbContext(readOptions));
        var entity1 = CmsEntity.CreatePublished(new CmsEvent { Id = "id1", Payload = "payload1", Version = 1, Timestamp = System.DateTimeOffset.UtcNow, Type = CmsEventType.Publish });
        var entity2 = CmsEntity.CreateUnpublished(new CmsEvent { Id = "id2", Payload = "payload2", Version = 2, Timestamp = System.DateTimeOffset.UtcNow, Type = CmsEventType.Unpublish });
        await repo.AddAsync(entity1);
        await repo.AddAsync(entity2);
        await repo.SaveChangesAsync();

        var dbContext = new CmsSyncDbContext(writeOptions);
        var published = await dbContext.CmsEntities.FirstOrDefaultAsync(e => e.Published);
        Assert.NotNull(published);
        Assert.Equal("id1", published!.Id);
    }
}
