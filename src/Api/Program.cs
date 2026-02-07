using Microsoft.EntityFrameworkCore;
using WcagAnalyzer.Domain.Repositories;
using WcagAnalyzer.Infrastructure.Data;
using WcagAnalyzer.Infrastructure.Repositories;

var builder = WebApplication.CreateBuilder(args);

// Database
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

// Repositories
builder.Services.AddScoped<IAnalysisRepository, AnalysisRepository>();

// MediatR
builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssembly(typeof(WcagAnalyzer.Application.AssemblyMarker).Assembly));

// CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("http://localhost:4200")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// OpenAPI / Swagger
builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseCors();

// Test endpoint
app.MapGet("/api/health", () => new
{
    Status = "OK",
    Timestamp = DateTime.UtcNow,
    Application = "WcagAnalyzer API"
});

app.Run();
