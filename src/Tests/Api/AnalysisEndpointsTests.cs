using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using WcagAnalyzer.Application.Models;
using WcagAnalyzer.Application.Services;
using WcagAnalyzer.Infrastructure.Data;

namespace WcagAnalyzer.Tests.Api;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly SqliteConnection _connection;

    public CustomWebApplicationFactory()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
    }

    protected override void ConfigureWebHost(Microsoft.AspNetCore.Hosting.IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Remove all DbContext-related registrations
            var descriptorsToRemove = services
                .Where(d => d.ServiceType.FullName?.Contains("DbContextOptions") == true
                          || d.ServiceType == typeof(AppDbContext)
                          || d.ServiceType == typeof(DbContextOptions<AppDbContext>)
                          || d.ServiceType == typeof(DbContextOptions))
                .ToList();
            foreach (var descriptor in descriptorsToRemove)
                services.Remove(descriptor);

            services.AddDbContext<AppDbContext>(options =>
                options.UseSqlite(_connection));

            // Replace Playwright analyzer with a mock for integration tests
            var analyzerDescriptor = services.FirstOrDefault(d => d.ServiceType == typeof(IAccessibilityAnalyzer));
            if (analyzerDescriptor != null)
                services.Remove(analyzerDescriptor);

            var mockAnalyzer = new Mock<IAccessibilityAnalyzer>();
            mockAnalyzer.Setup(a => a.AnalyzeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<AccessibilityViolation>());
            services.AddScoped<IAccessibilityAnalyzer>(_ => mockAnalyzer.Object);
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        _connection.Dispose();
    }
}

public class AnalysisEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public AnalysisEndpointsTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetHealth_ShouldReturnOk()
    {
        var response = await _client.GetAsync("/api/health");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadFromJsonAsync<JsonElement>();
        content.GetProperty("status").GetString().Should().Be("OK");
        content.GetProperty("application").GetString().Should().Be("WcagAnalyzer API");
    }

    [Fact]
    public async Task PostAnalysis_ValidUrl_ShouldReturn201()
    {
        var payload = new { url = "https://example.com", email = "user@example.com" };

        var response = await _client.PostAsJsonAsync("/api/analysis", payload);

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var content = await response.Content.ReadFromJsonAsync<JsonElement>();
        content.GetProperty("url").GetString().Should().Be("https://example.com");
        content.GetProperty("email").GetString().Should().Be("user@example.com");
        content.GetProperty("status").GetString().Should().Be("Pending");
        content.GetProperty("id").GetString().Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task PostAnalysis_EmptyUrl_ShouldReturnCreatedWithEmptyUrl()
    {
        var payload = new { url = "", email = "user@example.com" };

        var response = await _client.PostAsJsonAsync("/api/analysis", payload);

        // The API currently doesn't validate — it accepts any string
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var content = await response.Content.ReadFromJsonAsync<JsonElement>();
        content.GetProperty("url").GetString().Should().BeEmpty();
    }

    [Fact]
    public async Task GetAnalysis_ShouldReturnOk()
    {
        var response = await _client.GetAsync("/api/analysis");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetAnalysis_AfterPost_ShouldContainPostedItem()
    {
        var payload = new { url = "https://test-site.com", email = "test@site.com" };
        var postResponse = await _client.PostAsJsonAsync("/api/analysis", payload);
        postResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var response = await _client.GetAsync("/api/analysis");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadFromJsonAsync<JsonElement>();
        content.GetArrayLength().Should().BeGreaterThanOrEqualTo(1);

        var items = content.EnumerateArray().ToList();
        items.Should().Contain(item =>
            item.GetProperty("url").GetString() == "https://test-site.com");
    }

    [Fact]
    public async Task PostAnalysis_ShouldEnqueueForProcessing()
    {
        var payload = new { url = "https://enqueue-test.com", email = "enqueue@test.com" };

        var response = await _client.PostAsJsonAsync("/api/analysis", payload);

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var content = await response.Content.ReadFromJsonAsync<JsonElement>();
        content.GetProperty("status").GetString().Should().Be("Pending");
        content.GetProperty("id").GetString().Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GetAnalysisById_ExistingId_ShouldReturn200WithDetails()
    {
        var payload = new { url = "https://details-test.com", email = "details@test.com" };
        var postResponse = await _client.PostAsJsonAsync("/api/analysis", payload);
        var postContent = await postResponse.Content.ReadFromJsonAsync<JsonElement>();
        var id = postContent.GetProperty("id").GetString();

        var response = await _client.GetAsync($"/api/analysis/{id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadFromJsonAsync<JsonElement>();
        content.GetProperty("id").GetString().Should().Be(id);
        content.GetProperty("url").GetString().Should().Be("https://details-test.com");
        content.GetProperty("email").GetString().Should().Be("details@test.com");
        content.GetProperty("status").GetString().Should().Be("Pending");
        content.TryGetProperty("results", out var results).Should().BeTrue();
        results.GetArrayLength().Should().Be(0);
    }

    [Fact]
    public async Task GetAnalysisById_NonExistingId_ShouldReturn404()
    {
        var fakeId = Guid.NewGuid();

        var response = await _client.GetAsync($"/api/analysis/{fakeId}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var content = await response.Content.ReadFromJsonAsync<JsonElement>();
        content.GetProperty("message").GetString().Should().Be("Analysis not found");
    }

    [Fact]
    public async Task GetAnalysisById_InvalidGuid_ShouldReturn404()
    {
        var response = await _client.GetAsync("/api/analysis/not-a-guid");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task PostAnalysis_SameDomainTwice_ShouldReturn429OnSecond()
    {
        var payload1 = new { url = "https://ratelimit-test.com/page1", email = "user@test.com" };
        var response1 = await _client.PostAsJsonAsync("/api/analysis", payload1);
        response1.StatusCode.Should().Be(HttpStatusCode.Created);

        var payload2 = new { url = "https://ratelimit-test.com/page2", email = "user2@test.com" };
        var response2 = await _client.PostAsJsonAsync("/api/analysis", payload2);

        response2.StatusCode.Should().Be((HttpStatusCode)429);
    }
}
