using Microsoft.Playwright;

namespace UCK26.Ui.Tests;

public class LoginPageTests : BaseTests
{
    [Test]
    public async Task LoginPage_LoadsAndShowsForm()
    {
        await using var context = await CreateBrowserContextAsync();
        var page = await context.NewPageAsync();

        await page.GotoAsync(TestConfig.Route("/login"));

        await page.Locator("[data-test-id='login-form']").WaitForAsync(
            new LocatorWaitForOptions { Timeout = 10_000 });

        await Assert.That(await page.Locator("[data-test-id='login-username']").IsVisibleAsync()).IsTrue();
        await Assert.That(await page.Locator("[data-test-id='login-password']").IsVisibleAsync()).IsTrue();
        await Assert.That(await page.Locator("[data-test-id='login-submit']").IsVisibleAsync()).IsTrue();
    }

    [Test]
    public async Task Login_WithValidAdminCredentials_RedirectsToHomeAndShowsCurrentUser()
    {
        await using var context = await CreateBrowserContextAsync();
        var page = await context.NewPageAsync();

        await page.GotoAsync(TestConfig.Route("/login"));
        await page.Locator("[data-test-id='login-username']").FillAsync(TestConfig.AdminUserName);
        await page.Locator("[data-test-id='login-password']").FillAsync(TestConfig.AdminPassword);
        await page.Locator("[data-test-id='login-submit']").ClickAsync();

        await page.WaitForURLAsync($"{TestConfig.FrontendUrl}/", new PageWaitForURLOptions { Timeout = 10_000 });

        var currentUser = page.Locator("[data-test-id='current-user']");
        await currentUser.WaitForAsync(new LocatorWaitForOptions { Timeout = 5_000 });
        var text = await currentUser.InnerTextAsync();

        await Assert.That(text).Contains(TestConfig.AdminUserName);
        await Assert.That(text).Contains("Admin");
    }

    [Test]
    public async Task Login_WithWrongPassword_ShowsErrorAndStaysOnLogin()
    {
        await using var context = await CreateBrowserContextAsync();
        var page = await context.NewPageAsync();

        await page.GotoAsync(TestConfig.Route("/login"));
        await page.Locator("[data-test-id='login-username']").FillAsync(TestConfig.AdminUserName);
        await page.Locator("[data-test-id='login-password']").FillAsync("definitely-not-the-password");
        await page.Locator("[data-test-id='login-submit']").ClickAsync();

        await page.Locator("[data-test-id='login-error']").WaitForAsync(
            new LocatorWaitForOptions { Timeout = 5_000 });

        await Assert.That(page.Url).Contains("/login");
    }
}
