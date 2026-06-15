using System.Net;
using System.Text;
using FluentAssertions;

namespace WcagAnalyzer.Tests.Api;

public class WebhookEndpointTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public WebhookEndpointTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task PostWebhook_WithoutStripeSignatureHeader_ShouldReturn400()
    {
        var body = """{"type":"checkout.session.completed","data":{"object":{}}}""";
        var content = new StringContent(body, Encoding.UTF8, "application/json");

        var response = await _client.PostAsync("/api/webhook", content);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task PostWebhook_WithInvalidSignature_ShouldReturn400()
    {
        var body = """{"type":"checkout.session.completed","data":{"object":{}}}""";
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/webhook")
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json")
        };
        request.Headers.Add("Stripe-Signature", "t=invalid,v1=invalidsignature");

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task PostWebhook_WithEmptyBody_ShouldReturn400()
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/webhook")
        {
            Content = new StringContent("", Encoding.UTF8, "application/json")
        };
        request.Headers.Add("Stripe-Signature", "t=123,v1=abc");

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
