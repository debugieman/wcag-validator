using MediatR;
using Microsoft.EntityFrameworkCore;
using WcagAnalyzer.Application.Features.Analysis.Commands;
using WcagAnalyzer.Application.Features.Analysis.Queries;
using WcagAnalyzer.Application.Services;
using WcagAnalyzer.Domain.Repositories;
using WcagAnalyzer.Infrastructure.Data;
using Resend;
using WcagAnalyzer.Infrastructure.Repositories;
using WcagAnalyzer.Infrastructure.Services;
using Stripe;
using Stripe.Checkout;

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

// PDF report
builder.Services.AddScoped<IPdfReportGenerator, PdfReportGenerator>();

// Email
builder.Services.AddHttpClient();
builder.Services.Configure<ResendClientOptions>(options =>
    options.ApiToken = builder.Configuration["Resend:ApiKey"] ?? string.Empty);
builder.Services.AddTransient<IResend, ResendClient>();
builder.Services.AddScoped<IEmailSender, ResendEmailSender>();

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

// Stripe
StripeConfiguration.ApiKey = builder.Configuration["Stripe:SecretKey"];

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

// Stripe checkout — creates a Checkout Session and returns the redirect URL
app.MapPost("/api/checkout", async (CheckoutRequest req, IConfiguration config) =>
{
    var baseUrl = config["App:BaseUrl"] ?? "http://localhost:4200";
    var productName = req.DeepScan
        ? "WCAG Deep Scan — Accessibility Report"
        : "WCAG Quick Check — Accessibility Report";
    var productDesc = req.DeepScan
        ? "Full analysis of your main page plus up to 5 subpages"
        : "Single page accessibility analysis";

    var options = new SessionCreateOptions
    {
        PaymentMethodTypes = ["card"],
        LineItems =
        [
            new SessionLineItemOptions
            {
                PriceData = new SessionLineItemPriceDataOptions
                {
                    Currency = "usd",
                    UnitAmount = req.DeepScan ? 999L : 499L,
                    ProductData = new SessionLineItemPriceDataProductDataOptions
                    {
                        Name = productName,
                        Description = productDesc
                    }
                },
                Quantity = 1
            }
        ],
        Mode = "payment",
        CustomerEmail = req.Email,
        Metadata = new Dictionary<string, string>
        {
            ["url"]      = req.Url,
            ["email"]    = req.Email,
            ["deepScan"] = req.DeepScan.ToString()
        },
        SuccessUrl = $"{baseUrl}/success?email={Uri.EscapeDataString(req.Email)}",
        CancelUrl  = $"{baseUrl}/#analyzer"
    };

    var service = new SessionService();
    var session = await service.CreateAsync(options);

    return Results.Ok(new { url = session.Url });
});

// Stripe webhook — triggers analysis after successful payment
app.MapPost("/api/webhook", async (HttpRequest request, IMediator mediator, IConfiguration config, CancellationToken cancellationToken) =>
{
    var body = await new StreamReader(request.Body).ReadToEndAsync();
    var webhookSecret = config["Stripe:WebhookSecret"] ?? "";

    Event stripeEvent;
    try
    {
        stripeEvent = EventUtility.ConstructEvent(
            body,
            request.Headers["Stripe-Signature"],
            webhookSecret);
    }
    catch (StripeException)
    {
        return Results.BadRequest();
    }

    if (stripeEvent.Type == EventTypes.CheckoutSessionCompleted)
    {
        var session = stripeEvent.Data.Object as Session;
        if (session?.Metadata is not null &&
            session.Metadata.TryGetValue("url", out var url) &&
            session.Metadata.TryGetValue("email", out var email))
        {
            bool.TryParse(session.Metadata.GetValueOrDefault("deepScan", "false"), out var deepScan);
            await mediator.Send(new CreateAnalysisCommand(url, email, deepScan), cancellationToken);
        }
    }

    return Results.Ok();
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

app.MapGet("/api/analysis/summary", async (string email, IMediator mediator, CancellationToken cancellationToken) =>
{
    if (string.IsNullOrWhiteSpace(email))
        return Results.BadRequest();

    var result = await mediator.Send(new GetAnalysisSummaryByEmailQuery(email), cancellationToken);
    if (result is null)
        return Results.NotFound();

    return Results.Ok(result);
});

app.MapGet("/api/analysis", async (IMediator mediator, CancellationToken cancellationToken) =>
{
    return await mediator.Send(new GetAllAnalysesQuery(), cancellationToken);
});

app.MapGet("/api/analysis/{id:guid}/report", async (Guid id, IMediator mediator, IPdfReportGenerator pdfGenerator, CancellationToken cancellationToken) =>
{
    var result = await mediator.Send(new GetAnalysisByIdQuery(id), cancellationToken);
    if (result is null)
        return Results.NotFound(new { Message = "Analysis not found" });

    var hasResults = result.Results.Any();
    if (result.Status != "Completed" && !(result.Status == "Failed" && hasResults))
        return Results.Problem(detail: "Analysis is not completed yet.", statusCode: 409);

    var pdfBytes = pdfGenerator.Generate(result);
    var filename = $"wcag-report-{id}.pdf";
    return Results.File(pdfBytes, "application/pdf", filename);
});

app.Run();

public record CheckoutRequest(string Url, string Email, bool DeepScan);

public partial class Program { }
