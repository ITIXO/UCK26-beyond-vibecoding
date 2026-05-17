using Microsoft.Playwright;

namespace UCK26.Ui.Tests;

public class UsersPageTests : BaseTests
{
    [Test]
    public async Task UsersPage_AsAdmin_ListsAdminRow()
    {
        await using var context = await CreateAuthenticatedAdminContextAsync();
        var page = await context.NewPageAsync();

        await page.GotoAsync(TestConfig.Route("/users"));

        await page.Locator("[data-test-id='users-list-section']").WaitForAsync(
            new LocatorWaitForOptions { Timeout = 10_000 });

        var adminRow = page.Locator($"[data-test-id='user-row-{TestConfig.AdminUserName}']");
        await adminRow.WaitForAsync(new LocatorWaitForOptions { Timeout = 5_000 });
        await Assert.That(await adminRow.IsVisibleAsync()).IsTrue();
    }

    [Test]
    public async Task UsersPage_Unauthenticated_RedirectsToLogin()
    {
        await using var context = await CreateBrowserContextAsync();
        var page = await context.NewPageAsync();

        await page.GotoAsync(TestConfig.Route("/users"));

        await page.Locator("[data-test-id='login-form']").WaitForAsync(
            new LocatorWaitForOptions { Timeout = 10_000 });

        await Assert.That(page.Url).Contains("/login");
    }
}
