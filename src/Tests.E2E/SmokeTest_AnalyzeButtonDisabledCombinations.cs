using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;
using Xunit;

namespace WcagAnalyzer.Tests.E2E;

public class SmokeTest_AnalyzeButtonDisabledCombinations : IAsyncLifetime
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
    public async Task SmokeTest_AnalyzeButton_ShouldBeDisabled_WhenEmailIsEmptyAndUrlIsFilled()
    {
        await _page.GotoAsync("http://localhost:4200");

        var urlInput = _page.Locator("input[type='url']");
        var analyzeButton = _page.Locator("button", new() { HasText = "Analyze" });

        await urlInput.WaitForAsync();
        await urlInput.FillAsync("https://example.com");

        await Expect(analyzeButton).ToBeDisabledAsync();
    }

    [Fact]
    public async Task SmokeTest_AnalyzeButton_ShouldBeDisabled_WhenUrlIsEmptyAndEmailIsFilled()
    {
        await _page.GotoAsync("http://localhost:4200");

        var emailInput = _page.Locator("input[type='email']");
        var analyzeButton = _page.Locator("button", new() { HasText = "Analyze" });

        await emailInput.WaitForAsync();
        await emailInput.FillAsync("test@example.com");

        await Expect(analyzeButton).ToBeDisabledAsync();
    }
}
