using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;
using System.Text.Json;
using Xunit;

namespace WcagAnalyzer.Tests.E2E;

public class SmokeTest_CheckoutFlow : IAsyncLifetime
{
    private IPlaywright _playwright = null!;
    private IBrowser _browser = null!;
    private IPage _page = null!;

    public async Task InitializeAsync()
    {
        _playwright = await Playwright.CreateAsync();
        _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = true
        });
        _page = await _browser.NewPageAsync();
    }

    public async Task DisposeAsync()
    {
        await _browser.CloseAsync();
        _playwright.Dispose();
    }

    [Fact]
    public async Task SubmitForm_ShouldCallCheckoutEndpoint_WithCorrectPayload()
    {
        string? capturedBody = null;

        await _page.RouteAsync("**/api/checkout", async route =>
        {
            capturedBody = route.Request.PostData;
            await route.FulfillAsync(new RouteFulfillOptions
            {
                Status = 200,
                ContentType = "application/json",
                Body = """{"url":"http://localhost:4200/success?email=test%40example.com"}"""
            });
        });

        await _page.GotoAsync("http://localhost:4200");

        await _page.Locator("input[type='email']").FillAsync("test@example.com");
        await _page.Locator("input[type='url']").FillAsync("https://example.com");
        await _page.Locator("button", new() { HasText = "Get my report" }).ClickAsync();

        await _page.WaitForURLAsync("**/success**");

        Assert.NotNull(capturedBody);
        var payload = JsonDocument.Parse(capturedBody).RootElement;
        Assert.Equal("test@example.com", payload.GetProperty("email").GetString());
        Assert.Contains("example.com", payload.GetProperty("url").GetString());
        Assert.False(payload.GetProperty("deepScan").GetBoolean());
    }

    [Fact]
    public async Task SubmitForm_DeepScan_ShouldSendDeepScanTrue()
    {
        string? capturedBody = null;

        await _page.RouteAsync("**/api/checkout", async route =>
        {
            capturedBody = route.Request.PostData;
            await route.FulfillAsync(new RouteFulfillOptions
            {
                Status = 200,
                ContentType = "application/json",
                Body = """{"url":"http://localhost:4200/success?email=test%40example.com"}"""
            });
        });

        await _page.GotoAsync("http://localhost:4200");

        await _page.Locator("input[type='radio'][value='deep']").ClickAsync();
        await _page.Locator("input[type='email']").FillAsync("test@example.com");
        await _page.Locator("input[type='url']").FillAsync("https://example.com");
        await _page.Locator("button", new() { HasText = "Get my report" }).ClickAsync();

        await _page.WaitForURLAsync("**/success**");

        Assert.NotNull(capturedBody);
        var payload = JsonDocument.Parse(capturedBody).RootElement;
        Assert.True(payload.GetProperty("deepScan").GetBoolean());
    }

    [Fact]
    public async Task SubmitForm_OnCheckoutSuccess_ShouldRedirectToSuccessPage()
    {
        await _page.RouteAsync("**/api/checkout", async route =>
        {
            await route.FulfillAsync(new RouteFulfillOptions
            {
                Status = 200,
                ContentType = "application/json",
                Body = """{"url":"http://localhost:4200/success?email=test%40example.com"}"""
            });
        });

        await _page.GotoAsync("http://localhost:4200");

        await _page.Locator("input[type='email']").FillAsync("test@example.com");
        await _page.Locator("input[type='url']").FillAsync("https://example.com");
        await _page.Locator("button", new() { HasText = "Get my report" }).ClickAsync();

        await _page.WaitForURLAsync("**/success**");
        await Expect(_page.Locator(".success-container h1")).ToContainTextAsync("Payment successful");
    }

    [Fact]
    public async Task SubmitForm_OnCheckoutError_ShouldShowErrorMessage()
    {
        await _page.RouteAsync("**/api/checkout", async route =>
        {
            await route.FulfillAsync(new RouteFulfillOptions
            {
                Status = 500,
                ContentType = "application/json",
                Body = """{"error":"internal server error"}"""
            });
        });

        await _page.GotoAsync("http://localhost:4200");

        await _page.Locator("input[type='email']").FillAsync("test@example.com");
        await _page.Locator("input[type='url']").FillAsync("https://example.com");
        await _page.Locator("button", new() { HasText = "Get my report" }).ClickAsync();

        await Expect(_page.Locator("text=Something went wrong")).ToBeVisibleAsync();
    }
}
