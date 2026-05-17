using Microsoft.Playwright;

namespace UCK26.Ui.Tests;

public abstract class BaseTests
{
    [Before(Assembly, Order = 1)]
    public static Task EnsureBrowserInstalledAsync()
        => PlaywrightSetup.EnsureInstalledAsync("chromium");

    [BeforeEvery(Test, Order = 1)]
    public static async Task EnsureFrontendReachableAsync()
    {
        using var client = new HttpClient();
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        try
        {
            using var response = await client.GetAsync(TestConfig.FrontendUrl, cts.Token);
            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException($"Frontend not healthy at {TestConfig.FrontendUrl} (status {(int)response.StatusCode}).");
            }
        }
        catch (Exception ex) when (ex is OperationCanceledException or HttpRequestException)
        {
            throw new InvalidOperationException($"Frontend not reachable at {TestConfig.FrontendUrl}.", ex);
        }
    }

    protected static async Task<IBrowserContext> CreateBrowserContextAsync(bool headless = true, bool recordVideo = false)
    {
        var playwright = await Playwright.CreateAsync();
        var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = headless });
        var contextOptions = recordVideo ? new BrowserNewContextOptions
        {
            RecordVideoDir = "videos/",
            RecordVideoSize = new RecordVideoSize { Width = 1280, Height = 720 }
        } : null;
        return await browser.NewContextAsync(contextOptions);
    }

    protected static async Task<IBrowserContext> CreateAuthenticatedAdminContextAsync(bool headless = true)
    {
        var context = await CreateBrowserContextAsync(headless);
        var storageState = await AuthSetup.GetAdminStorageStateAsync(headless);
        await context.AddInitScriptAsync($$"""
            window.localStorage.setItem("uck26.token", {{System.Text.Json.JsonSerializer.Serialize(storageState.Token)}});
        """);
        return context;
    }
}
