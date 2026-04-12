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
builder.Services.AddScoped<ICmsEventApplicationService, CmsEventApplicationService>();
builder.Services.AddScoped<ICmsEntityRepository, CmsEntityRepository>();
builder.Services.AddScoped<ICmsEntityAdminService, CmsEntityAdminService>();

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();
app.MapGet("/health", [AllowAnonymous] () => Results.Text("Healthy"));
app.MapControllers();
app.Run();

public partial class Program { }
