var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

var app = builder.Build();

app.MapGet("/health", () => Results.Text("Healthy"));
app.MapControllers();

app.Run();

public partial class Program { }
