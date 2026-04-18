using CmsSyncService.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace CmsSyncService.Api.Tests.TestFixtures;

public sealed class TestWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly SqliteConnection _connection;

    public TestWebApplicationFactory()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            services.RemoveAll(typeof(DbContextOptions<CmsSyncDbContext>));
            services.RemoveAll(typeof(DbContextOptions<CmsSyncReadDbContext>));
            services.RemoveAll(typeof(CmsSyncDbContext));
            services.RemoveAll(typeof(CmsSyncReadDbContext));

            services.AddDbContext<CmsSyncDbContext>(options =>
                options.UseSqlite(_connection));
            services.AddDbContext<CmsSyncReadDbContext>(options =>
            {
                options.UseSqlite(_connection);
                options.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
            });

            using var scope = services.BuildServiceProvider().CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<CmsSyncDbContext>();
            dbContext.Database.EnsureCreated();
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing)
        {
            _connection.Dispose();
        }
    }
}
