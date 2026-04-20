using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using CmsSyncService.Application.Repositories;
using CmsSyncService.Application.Services;
using CmsSyncService.Infrastructure.Persistence;
using CmsSyncService.Application.Caching;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;


var builder = WebApplication.CreateBuilder(args);

// Enforce 1MB request body size limit for Kestrel
builder.WebHost.ConfigureKestrel(options =>
{
	options.Limits.MaxRequestBodySize = 1048576; // 1 MB
});

builder.Services.AddAuthentication(options =>
{
	options.DefaultAuthenticateScheme = "BasicAuthentication";
	options.DefaultChallengeScheme = "BasicAuthentication";
}).AddScheme<AuthenticationSchemeOptions, BasicAuthHandler>("BasicAuthentication", null);
builder.Services.AddControllers();
	builder.Services.AddMemoryCache();
builder.Services.Configure<CmsSyncService.Application.CacheDurations>(builder.Configuration.GetSection("CacheDurations"));
builder.Services.AddSingleton<IEntityCacheService, EntityCacheService>();

// Dependency Injection registration
builder.Services.AddDbContext<CmsSyncDbContext>(options =>
	options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddDbContext<CmsSyncReadDbContext>(options =>
{
	options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"));
	options.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
});
builder.Services.AddScoped<ICmsEventApplicationService, CmsEventApplicationService>();
builder.Services.AddScoped<ICmsEntityRepository, CmsEntityRepository>();
builder.Services.AddScoped<ICmsEntityAdminService, CmsEntityAdminService>();


var app = builder.Build();


// WARNING: Basic Auth credentials are only secure over HTTPS. Do not deploy without TLS.
if (!app.Environment.IsDevelopment())
{
	app.UseHttpsRedirection();
}
app.UseAuthentication();
app.UseAuthorization();
app.MapGet("/health", [AllowAnonymous] () => Results.Text("Healthy"));
app.MapControllers();
app.Run();

public partial class Program { }
