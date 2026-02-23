using MediatR;
using Microsoft.EntityFrameworkCore;
using WcagAnalyzer.Application.Features.Analysis.Commands;
using WcagAnalyzer.Application.Features.Analysis.Queries;
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

// Rate limiting
builder.Services.AddScoped<IRateLimiter, AnalysisRateLimiter>();

// Background processing
builder.Services.AddSingleton<IAnalysisQueue, AnalysisQueue>();
builder.Services.AddSingleton<PlaywrightBrowserManager>();
builder.Services.AddScoped<IAccessibilityAnalyzer, PlaywrightAccessibilityAnalyzer>();
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
        policy.WithOrigins("http://localhost:4200", "http://localhost")
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
app.MapPost("/api/analysis", async (CreateAnalysisCommand command, IMediator mediator, CancellationToken cancellationToken) =>
{
    var result = await mediator.Send(command, cancellationToken);
    if (result.IsRateLimited)
    {
        return Results.Problem(
            detail: "This domain was already analyzed in the last 24 hours.",
            statusCode: 429);
    }

    return Results.Created($"/api/analysis/{result.Id}", new
    {
        result.Id,
        result.Url,
        result.Email,
        result.Status,
        result.CreatedAt
    });
});

app.MapGet("/api/analysis/{id:guid}", async (Guid id, IMediator mediator, CancellationToken cancellationToken) =>
{
    var result = await mediator.Send(new GetAnalysisByIdQuery(id), cancellationToken);
    if (result is null)
        return Results.NotFound(new { Message = "Analysis not found" });

    return Results.Ok(result);
});

app.MapGet("/api/analysis", async (IMediator mediator, CancellationToken cancellationToken) =>
{
    return await mediator.Send(new GetAllAnalysesQuery(), cancellationToken);
});

app.Run();

public partial class Program { }
