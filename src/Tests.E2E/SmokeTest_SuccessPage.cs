using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;
using Xunit;

namespace WcagAnalyzer.Tests.E2E;

public class SmokeTest_SuccessPage : IAsyncLifetime
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
    public async Task SuccessPage_ShouldShowEmailFromQueryParam()
    {
        await _page.GotoAsync("http://localhost:4200/success?email=test%40example.com");

        await Expect(_page.Locator("text=test@example.com")).ToBeVisibleAsync();
    }

    [Fact]
    public async Task SuccessPage_ShouldShowConfirmationHeading()
    {
        await _page.GotoAsync("http://localhost:4200/success?email=test%40example.com");

        await Expect(_page.Locator(".success-container h1")).ToContainTextAsync("Payment successful");
    }

    [Fact]
    public async Task SuccessPage_ShouldHaveScanAnotherWebsiteLink()
    {
        await _page.GotoAsync("http://localhost:4200/success?email=test%40example.com");

        var link = _page.Locator("a", new() { HasText = "Scan another website" });
        await Expect(link).ToBeVisibleAsync();
    }

    [Fact]
    public async Task SuccessPage_ScanAnotherWebsiteLink_ShouldPointToHomePage()
    {
        await _page.GotoAsync("http://localhost:4200/success?email=test%40example.com");

        var link = _page.Locator("a", new() { HasText = "Scan another website" });
        await link.ClickAsync();

        await Expect(_page).ToHaveURLAsync("http://localhost:4200/");
    }

    [Fact]
    public async Task SuccessPage_WithoutEmail_ShouldRenderWithoutError()
    {
        await _page.GotoAsync("http://localhost:4200/success");

        await Expect(_page.Locator(".success-container h1")).ToContainTextAsync("Payment successful");
    }
}
