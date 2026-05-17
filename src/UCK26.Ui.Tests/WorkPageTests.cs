using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Playwright;

namespace UCK26.Ui.Tests;

public class WorkPageTests : BaseTests
{
    [Test]
    public async Task HomePage_WorkCard_NavigatesToWorkPage()
    {
        await using var context = await CreateAuthenticatedAdminContextAsync();
        var page = await context.NewPageAsync();

        await page.GotoAsync(TestConfig.Route("/"));
        await page.Locator("[data-test-id='work-card']").WaitForAsync(
            new LocatorWaitForOptions { Timeout = 10_000 });
        await page.Locator("[data-test-id='work-card-link']").ClickAsync();

        await page.WaitForURLAsync($"{TestConfig.FrontendUrl}/work", new PageWaitForURLOptions { Timeout = 10_000 });
        await page.Locator("[data-test-id='work-sheet']").WaitForAsync(
            new LocatorWaitForOptions { Timeout = 10_000 });
    }

    [Test]
    public async Task WorkPage_AsAdmin_AddsWorkEntry()
    {
        var date = new DateOnly(DateTime.Today.Year, DateTime.Today.Month, 15);
        await DeleteEntriesForDateAsync(date);

        await using var context = await CreateAuthenticatedAdminContextAsync();
        var page = await context.NewPageAsync();

        await page.GotoAsync(TestConfig.Route("/work"));
        await page.Locator("[data-test-id='work-sheet']").WaitForAsync(
            new LocatorWaitForOptions { Timeout = 10_000 });

        var key = date.ToString("yyyy-MM-dd");
        await page.Locator($"[data-test-id='work-add-{key}-work']").ClickAsync();
        await page.Locator("[data-test-id='work-entry-dialog']").WaitForAsync(
            new LocatorWaitForOptions { Timeout = 5_000 });
        await page.Locator("[data-test-id='work-entry-start']").FillAsync("08:15");
        await page.Locator("[data-test-id='work-entry-end']").FillAsync("10:45");
        await page.Locator("[data-test-id='work-entry-description']").FillAsync("UI test entry");
        await page.Locator("[data-test-id='work-entry-submit']").ClickAsync();

        await page.Locator($"[data-test-id='work-cell-{key}-work']").WaitForAsync(
            new LocatorWaitForOptions { Timeout = 10_000 });
        await Assert.That(await page.Locator($"[data-test-id='work-start-{key}']").InnerTextAsync()).IsEqualTo("08:15");
        await Assert.That(await page.Locator($"[data-test-id='work-end-{key}']").InnerTextAsync()).IsEqualTo("10:45");
        await Assert.That(await page.Locator($"[data-test-id='work-cell-{key}-work']").InnerTextAsync()).Contains("02:30");
    }

    [Test]
    public async Task WorkPage_Unauthenticated_RedirectsToLogin()
    {
        await using var context = await CreateBrowserContextAsync();
        var page = await context.NewPageAsync();

        await page.GotoAsync(TestConfig.Route("/work"));

        await page.Locator("[data-test-id='login-form']").WaitForAsync(
            new LocatorWaitForOptions { Timeout = 10_000 });
        await Assert.That(page.Url).Contains("/login");
    }

    private static async Task DeleteEntriesForDateAsync(DateOnly date)
    {
        var storage = await AuthSetup.GetAdminStorageStateAsync();
        using var client = new HttpClient { BaseAddress = new Uri(TestConfig.ApiUrl) };
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", storage.Token);

        var worksheet = await client.GetFromJsonAsync<JsonElement>($"/api/worksheets?year={date.Year}&month={date.Month}");
        foreach (var entry in worksheet.GetProperty("entries").EnumerateArray()
                     .Where(entry => entry.GetProperty("date").GetString() == date.ToString("yyyy-MM-dd")))
        {
            var id = entry.GetProperty("id").GetInt32();
            await client.DeleteAsync($"/api/worksheets/entries/{id}");
        }
    }
}
