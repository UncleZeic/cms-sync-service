
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAuthentication("BasicAuthentication").AddScheme<AuthenticationSchemeOptions, BasicAuthHandler>("BasicAuthentication", null);
builder.Services.AddControllers();

// Dependency Injection registration
builder.Services.AddDbContext<CmsSyncService.Infrastructure.Persistence.CmsSyncDbContext>(options =>
	options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddScoped<CmsSyncService.Application.Services.ICmsEventApplicationService, CmsSyncService.Application.Services.CmsEventApplicationService>();
builder.Services.AddScoped<CmsSyncService.Application.Repositories.ICmsEntityRepository, CmsSyncService.Infrastructure.Persistence.CmsEntityRepository>();

var app = builder.Build();

app.UseAuthentication();
app.MapGet("/health", [Microsoft.AspNetCore.Authorization.AllowAnonymous] () => Results.Text("Healthy"));
app.MapControllers();
app.Run();

public partial class Program { }
