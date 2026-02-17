using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using FluentAssertions;

namespace WcagAnalyzer.Tests.Api;

public class HttpClientApiTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public HttpClientApiTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task PostAnalysis_WithManualHttpRequestMessage_ShouldReturn201()
    {
        var json = JsonSerializer.Serialize(new { url = "https://httpclient-test.com" });
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/analysis")
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("url").GetString().Should().Be("https://httpclient-test.com");
        body.GetProperty("status").GetString().Should().Be("Pending");
    }

    [Fact]
    public async Task PostAnalysis_ShouldReturnLocationHeader()
    {
        var json = JsonSerializer.Serialize(new { url = "https://location-header-test.com" });
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/analysis")
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location.Should().NotBeNull();
        response.Headers.Location!.ToString().Should().StartWith("/api/analysis/");
    }

    [Fact]
    public async Task PostAnalysis_ResponseShouldHaveJsonContentType()
    {
        var json = JsonSerializer.Serialize(new { url = "https://content-type-test.com" });
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/analysis")
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };

        var response = await _client.SendAsync(request);

        response.Content.Headers.ContentType.Should().NotBeNull();
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/json");
    }

    [Fact]
    public async Task PostAnalysis_WithInvalidJson_ShouldReturnBadRequest()
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/analysis")
        {
            Content = new StringContent("{ invalid json }", Encoding.UTF8, "application/json")
        };

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task PostAnalysis_WithoutContentType_ShouldReturnUnsupportedMediaType()
    {
        var json = JsonSerializer.Serialize(new { url = "https://no-content-type.com" });
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/analysis")
        {
            Content = new StringContent(json, Encoding.UTF8)
        };
        request.Content.Headers.ContentType = null;

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.UnsupportedMediaType,
            HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetAnalysisById_WithManualRequest_ShouldReturnFullDetails()
    {
        // Arrange — create analysis first
        var postResponse = await _client.PostAsJsonAsync("/api/analysis", new { url = "https://manual-get-test.com" });
        var postBody = await postResponse.Content.ReadFromJsonAsync<JsonElement>();
        var id = postBody.GetProperty("id").GetString();

        // Act — GET with manual HttpRequestMessage
        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/analysis/{id}");
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        var response = await _client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/json");

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("id").GetString().Should().Be(id);
        body.GetProperty("url").GetString().Should().Be("https://manual-get-test.com");
        body.TryGetProperty("createdAt", out _).Should().BeTrue();
        body.TryGetProperty("completedAt", out _).Should().BeTrue();
        body.TryGetProperty("errorMessage", out _).Should().BeTrue();
        body.TryGetProperty("results", out _).Should().BeTrue();
    }

    [Fact]
    public async Task PostAndGet_FullFlow_ShouldReturnConsistentData()
    {
        // POST — create
        var json = JsonSerializer.Serialize(new { url = "https://full-flow-test.com" });
        var postRequest = new HttpRequestMessage(HttpMethod.Post, "/api/analysis")
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
        var postResponse = await _client.SendAsync(postRequest);
        var postBody = await postResponse.Content.ReadFromJsonAsync<JsonElement>();
        var id = postBody.GetProperty("id").GetString();

        // GET by ID
        var getByIdRequest = new HttpRequestMessage(HttpMethod.Get, $"/api/analysis/{id}");
        var getByIdResponse = await _client.SendAsync(getByIdRequest);
        var getByIdBody = await getByIdResponse.Content.ReadFromJsonAsync<JsonElement>();

        // GET list
        var getListRequest = new HttpRequestMessage(HttpMethod.Get, "/api/analysis");
        var getListResponse = await _client.SendAsync(getListRequest);
        var getListBody = await getListResponse.Content.ReadFromJsonAsync<JsonElement>();

        // Assert — data is consistent across endpoints
        getByIdBody.GetProperty("id").GetString().Should().Be(id);
        getByIdBody.GetProperty("url").GetString().Should().Be("https://full-flow-test.com");

        var listItems = getListBody.EnumerateArray().ToList();
        listItems.Should().Contain(item =>
            item.GetProperty("id").GetString() == id &&
            item.GetProperty("url").GetString() == "https://full-flow-test.com");
    }

    [Fact]
    public async Task DeleteAnalysis_ShouldReturnMethodNotAllowed()
    {
        var request = new HttpRequestMessage(HttpMethod.Delete, $"/api/analysis/{Guid.NewGuid()}");

        var response = await _client.SendAsync(request);

        // We don't have DELETE endpoint — should return 404 or 405
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.NotFound,
            HttpStatusCode.MethodNotAllowed);
    }
}
