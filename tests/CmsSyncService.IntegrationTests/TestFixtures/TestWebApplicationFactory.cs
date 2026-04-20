using CmsSyncService.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Linq;

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
            var efRegistrations = services
                .Where(descriptor =>
                    descriptor.ServiceType == typeof(DbContextOptions) ||
                    descriptor.ServiceType == typeof(DbContextOptions<CmsSyncDbContext>) ||
                    descriptor.ServiceType == typeof(DbContextOptions<CmsSyncReadDbContext>) ||
                    descriptor.ServiceType == typeof(CmsSyncDbContext) ||
                    descriptor.ServiceType == typeof(CmsSyncReadDbContext) ||
                    descriptor.ServiceType.FullName?.StartsWith("Microsoft.EntityFrameworkCore.Infrastructure.IDbContextOptionsConfiguration") == true)
                .ToList();

            foreach (var registration in efRegistrations)
            {
                services.Remove(registration);
            }

            services.RemoveAll(typeof(DbContextOptions<CmsSyncDbContext>));
            services.RemoveAll(typeof(DbContextOptions<CmsSyncReadDbContext>));
            services.RemoveAll(typeof(DbContextOptions));
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
