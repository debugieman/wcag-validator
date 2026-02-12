using Microsoft.EntityFrameworkCore;
using WcagAnalyzer.Application.Services;
using WcagAnalyzer.Domain.Repositories;
using WcagAnalyzer.Infrastructure.Data;
using WcagAnalyzer.Infrastructure.Repositories;
using WcagAnalyzer.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);

// Database
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

// Repositories
builder.Services.AddScoped<IAnalysisRepository, AnalysisRepository>();

// Background processing
builder.Services.AddSingleton<IAnalysisQueue, AnalysisQueue>();
builder.Services.AddScoped<IAnalysisProcessor, AnalysisProcessor>();
builder.Services.AddHostedService<AnalysisBackgroundService>();

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

// Apply pending migrations on startup
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

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

// Analysis endpoints
app.MapPost("/api/analysis", async (AnalysisCreateRequest request, IAnalysisRepository repo, IAnalysisQueue queue) =>
{
    var analysis = new WcagAnalyzer.Domain.Entities.AnalysisRequest
    {
        Id = Guid.NewGuid(),
        Url = request.Url,
        Status = WcagAnalyzer.Domain.Enums.AnalysisStatus.Pending,
        CreatedAt = DateTime.UtcNow
    };

    await repo.AddAsync(analysis);
    await queue.EnqueueAsync(analysis.Id);

    return Results.Created($"/api/analysis/{analysis.Id}", new
    {
        analysis.Id,
        analysis.Url,
        Status = analysis.Status.ToString(),
        analysis.CreatedAt
    });
});

app.MapGet("/api/analysis", async (IAnalysisRepository repo) =>
{
    var all = await repo.GetAllAsync();
    return all.Select(a => new
    {
        a.Id,
        a.Url,
        Status = a.Status.ToString(),
        a.CreatedAt
    });
});

app.Run();

record AnalysisCreateRequest(string Url);

public partial class Program { }
