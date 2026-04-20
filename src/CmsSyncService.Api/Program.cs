using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using CmsSyncService.Application;
using CmsSyncService.Application.Repositories;
using CmsSyncService.Application.Services;
using CmsSyncService.Infrastructure.Persistence;
using CmsSyncService.Application.Caching;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;


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
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
	options.SwaggerDoc("v1", new OpenApiInfo
	{
		Title = "CMS Sync Service API",
		Version = "v1"
	});

	options.AddSecurityDefinition("Basic", new OpenApiSecurityScheme
	{
		Description = "Basic Authentication header using the Basic scheme.",
		Name = "Authorization",
		In = ParameterLocation.Header,
		Type = SecuritySchemeType.Http,
		Scheme = "basic"
	});

	options.AddSecurityRequirement(new OpenApiSecurityRequirement
	{
		{
			new OpenApiSecurityScheme
			{
				Reference = new OpenApiReference
				{
					Type = ReferenceType.SecurityScheme,
					Id = "Basic"
				}
			},
			Array.Empty<string>()
		}
	});
});
builder.Services.Configure<CacheDurations>(builder.Configuration.GetSection("CacheDurations"));
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


if (app.Environment.IsDevelopment())
{
	app.UseSwagger();
	app.UseSwaggerUI();
}

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
