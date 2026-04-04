using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;
using Xunit;

namespace WcagAnalyzer.Tests.E2E;

public class SmokeTest_AnalyzeButtonDisabled : IAsyncLifetime
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
    public async Task SmokeTest_AnalyzeButton_ShouldBeDisabled_WhenUrlIsEmpty()
    {
        await _page.GotoAsync("http://localhost:4200");

        var analyzeButton = _page.Locator("button", new() { HasText = "Analyze" });
        await analyzeButton.WaitForAsync();

        await Expect(analyzeButton).ToBeDisabledAsync();
    }
}
