using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using Stripe.Checkout;

namespace WcagAnalyzer.Tests.Api;

public class CheckoutEndpointTests : IDisposable
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;
    private readonly Mock<IStripeCheckoutSessionService> _checkoutMock = new();
    private SessionCreateOptions? _capturedOptions;

    public CheckoutEndpointTests()
    {
        _checkoutMock
            .Setup(c => c.CreateAsync(It.IsAny<SessionCreateOptions>(), It.IsAny<CancellationToken>()))
            .Callback<SessionCreateOptions, CancellationToken>((options, _) => _capturedOptions = options)
            .ReturnsAsync(new Session { Url = "https://checkout.stripe.com/pay/cs_test_123" });

        _factory = new CustomWebApplicationFactory();
        _client = _factory.WithWebHostBuilder(b =>
            b.ConfigureServices(services =>
            {
                services.RemoveAll<IStripeCheckoutSessionService>();
                services.AddScoped(_ => _checkoutMock.Object);
            })).CreateClient();
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    [Fact]
    public async Task PostCheckout_ShouldReturnCheckoutUrlFromStripe()
    {
        var response = await _client.PostAsJsonAsync("/api/checkout",
            new { url = "https://example.com", email = "user@example.com", deepScan = false });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>();
        body!["url"].Should().Be("https://checkout.stripe.com/pay/cs_test_123");
    }

    [Theory]
    [InlineData(false, 499L)]
    [InlineData(true, 999L)]
    public async Task PostCheckout_ShouldChargeCorrectAmountForScanType(bool deepScan, long expectedCents)
    {
        await _client.PostAsJsonAsync("/api/checkout",
            new { url = "https://example.com", email = "user@example.com", deepScan });

        _capturedOptions!.LineItems.Should().ContainSingle()
            .Which.PriceData.UnitAmount.Should().Be(expectedCents);
    }

    [Fact]
    public async Task PostCheckout_QuickScan_ShouldUseQuickScanProductName()
    {
        await _client.PostAsJsonAsync("/api/checkout",
            new { url = "https://example.com", email = "user@example.com", deepScan = false });

        _capturedOptions!.LineItems.Should().ContainSingle()
            .Which.PriceData.ProductData.Name.Should().Be("WCAG Quick Check — Accessibility Report");
    }

    [Fact]
    public async Task PostCheckout_DeepScan_ShouldUseDeepScanProductName()
    {
        await _client.PostAsJsonAsync("/api/checkout",
            new { url = "https://example.com", email = "user@example.com", deepScan = true });

        _capturedOptions!.LineItems.Should().ContainSingle()
            .Which.PriceData.ProductData.Name.Should().Be("WCAG Deep Scan — Accessibility Report");
    }

    [Fact]
    public async Task PostCheckout_ShouldPassUrlEmailAndDeepScanAsMetadata()
    {
        await _client.PostAsJsonAsync("/api/checkout",
            new { url = "https://example.com", email = "user@example.com", deepScan = true });

        _capturedOptions!.Metadata.Should().Contain(new KeyValuePair<string, string>("url", "https://example.com"));
        _capturedOptions!.Metadata.Should().Contain(new KeyValuePair<string, string>("email", "user@example.com"));
        _capturedOptions!.Metadata.Should().Contain(new KeyValuePair<string, string>("deepScan", "True"));
    }

    [Fact]
    public async Task PostCheckout_ShouldSetCustomerEmail()
    {
        await _client.PostAsJsonAsync("/api/checkout",
            new { url = "https://example.com", email = "user@example.com", deepScan = false });

        _capturedOptions!.CustomerEmail.Should().Be("user@example.com");
    }

    [Fact]
    public async Task PostCheckout_ShouldPointSuccessUrlAtSuccessPageWithEmail()
    {
        await _client.PostAsJsonAsync("/api/checkout",
            new { url = "https://example.com", email = "user+test@example.com", deepScan = false });

        _capturedOptions!.SuccessUrl.Should().Contain("/success?email=user%2Btest%40example.com");
    }

    [Fact]
    public async Task PostCheckout_ShouldPointCancelUrlBackToAnalyzer()
    {
        await _client.PostAsJsonAsync("/api/checkout",
            new { url = "https://example.com", email = "user@example.com", deepScan = false });

        _capturedOptions!.CancelUrl.Should().EndWith("/#analyzer");
    }

    [Fact]
    public async Task PostCheckout_ShouldUsePaymentMode()
    {
        await _client.PostAsJsonAsync("/api/checkout",
            new { url = "https://example.com", email = "user@example.com", deepScan = false });

        _capturedOptions!.Mode.Should().Be("payment");
    }
}
