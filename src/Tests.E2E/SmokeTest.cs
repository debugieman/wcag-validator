  using Microsoft.Playwright;                                                                       
  using static Microsoft.Playwright.Assertions;
  using Xunit;

  namespace WcagAnalyzer.Tests.E2E;

  public class SmokeTests : IAsyncLifetime
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
      public async Task SmokeTest_EmailAndUrlInputs_ShouldBeVisible()
      {
          await _page.GotoAsync("http://localhost:4200");

          var emailInput = _page.Locator("input[type='email']");
          var urlInput = _page.Locator("input[type='url']");

          await emailInput.WaitForAsync();
          await urlInput.WaitForAsync();

          await emailInput.ClickAsync();
          await urlInput.ClickAsync();

          await Expect(emailInput).ToBeVisibleAsync();
          await Expect(urlInput).ToBeVisibleAsync();
      }

      [Fact]
      public async Task SmokeTest_DeepScanRadio_ShouldBeCheckedAfterClick()
      {
          await _page.GotoAsync("http://localhost:4200");

          var deepScanRadio = _page.Locator("input[type='radio'][value='deep']");
          await deepScanRadio.WaitForAsync();

          await deepScanRadio.ClickAsync();

          await Expect(deepScanRadio).ToBeCheckedAsync();
      }
  }
