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
        await page.Locator("[data-test-id='work-user-selector']").WaitForAsync(
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
        await Assert.That(await page.Locator("[data-test-id='work-summary-work']").IsVisibleAsync()).IsTrue();
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

    // Audit sidebar tests (#6 and #7)

    [Test]
    public async Task WorkPage_HistoryButton_OpensSidebar()
    {
        var date = new DateOnly(DateTime.Today.Year, DateTime.Today.Month, 10);
        var key = date.ToString("yyyy-MM-dd");

        await using var context = await CreateAuthenticatedAdminContextAsync();
        var page = await context.NewPageAsync();
        await page.GotoAsync(TestConfig.Route("/work"));
        await page.Locator("[data-test-id='work-sheet']").WaitForAsync(new LocatorWaitForOptions { Timeout = 10_000 });

        await page.Locator($"[data-test-id='work-history-{key}']").ClickAsync();
        await page.Locator("[data-test-id='work-history-sidebar']").WaitForAsync(
            new LocatorWaitForOptions { Timeout = 5_000, State = WaitForSelectorState.Visible });
        await Assert.That(await page.Locator("[data-test-id='work-history-sidebar']").IsVisibleAsync()).IsTrue();
    }

    [Test]
    public async Task WorkPage_HistoryButton_ShowsAuditEventAfterEntryCreated()
    {
        var date = new DateOnly(DateTime.Today.Year, DateTime.Today.Month, 11);
        await DeleteEntriesForDateAsync(date);
        await CreateEntryForDateAsync(date, "work");

        var key = date.ToString("yyyy-MM-dd");

        await using var context = await CreateAuthenticatedAdminContextAsync();
        var page = await context.NewPageAsync();
        await page.GotoAsync(TestConfig.Route("/work"));
        await page.Locator("[data-test-id='work-sheet']").WaitForAsync(new LocatorWaitForOptions { Timeout = 10_000 });

        await page.Locator($"[data-test-id='work-history-{key}']").ClickAsync();
        await page.Locator("[data-test-id='work-audit-event-0']").WaitForAsync(
            new LocatorWaitForOptions { Timeout = 5_000, State = WaitForSelectorState.Visible });
        await Assert.That(await page.Locator("[data-test-id='work-audit-event-0']").IsVisibleAsync()).IsTrue();
    }

    [Test]
    public async Task WorkPage_HistoryButton_NoEntries_ShowsEmptyMessage()
    {
        var date = new DateOnly(DateTime.Today.Year, DateTime.Today.Month, 12);
        await DeleteEntriesForDateAsync(date);

        var key = date.ToString("yyyy-MM-dd");

        await using var context = await CreateAuthenticatedAdminContextAsync();
        var page = await context.NewPageAsync();
        await page.GotoAsync(TestConfig.Route("/work"));
        await page.Locator("[data-test-id='work-sheet']").WaitForAsync(new LocatorWaitForOptions { Timeout = 10_000 });

        await page.Locator($"[data-test-id='work-history-{key}']").ClickAsync();
        await page.Locator("[data-test-id='work-history-sidebar']").WaitForAsync(
            new LocatorWaitForOptions { Timeout = 5_000, State = WaitForSelectorState.Visible });
        await page.GetByText("No history for this day.").WaitForAsync(new LocatorWaitForOptions { Timeout = 5_000 });
        await Assert.That(await page.GetByText("No history for this day.").IsVisibleAsync()).IsTrue();
    }

    [Test]
    public async Task WorkPage_HistoryButton_ClickSameButton_ClosesSidebar()
    {
        var date = new DateOnly(DateTime.Today.Year, DateTime.Today.Month, 13);
        var key = date.ToString("yyyy-MM-dd");

        await using var context = await CreateAuthenticatedAdminContextAsync();
        var page = await context.NewPageAsync();
        await page.GotoAsync(TestConfig.Route("/work"));
        await page.Locator("[data-test-id='work-sheet']").WaitForAsync(new LocatorWaitForOptions { Timeout = 10_000 });

        await page.Locator($"[data-test-id='work-history-{key}']").ClickAsync();
        await page.Locator("[data-test-id='work-history-sidebar']").WaitForAsync(
            new LocatorWaitForOptions { Timeout = 5_000, State = WaitForSelectorState.Visible });

        await page.Locator($"[data-test-id='work-history-{key}']").ClickAsync();
        await page.Locator("[data-test-id='work-history-sidebar']").WaitForAsync(
            new LocatorWaitForOptions { Timeout = 5_000, State = WaitForSelectorState.Hidden });
        await Assert.That(await page.Locator("[data-test-id='work-history-sidebar']").IsVisibleAsync()).IsFalse();
    }

    [Test]
    public async Task WorkPage_HistoryButton_ExistsOnWeekendRows()
    {
        var firstSunday = Enumerable.Range(1, DateTime.DaysInMonth(year, month))
            .Select(d => new DateOnly(DateTime.Today.Year, DateTime.Today.Month, d))
            .FirstOrDefault(d => d.DayOfWeek == DayOfWeek.Sunday);

        if (firstSunday == default) return;

        var key = firstSunday.ToString("yyyy-MM-dd");

        await using var context = await CreateAuthenticatedAdminContextAsync();
        var page = await context.NewPageAsync();
        await page.GotoAsync(TestConfig.Route("/work"));
        await page.Locator("[data-test-id='work-sheet']").WaitForAsync(new LocatorWaitForOptions { Timeout = 10_000 });

        await Assert.That(await page.Locator($"[data-test-id='work-history-{key}']").IsVisibleAsync()).IsTrue();
    }

    private static async Task CreateEntryForDateAsync(DateOnly date, string type)
    {
        var storage = await AuthSetup.GetAdminStorageStateAsync();
        using var client = new HttpClient { BaseAddress = new Uri(TestConfig.ApiUrl) };
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", storage.Token);

        await client.PostAsJsonAsync("/api/worksheets/entries", new
        {
            date = date.ToString("yyyy-MM-dd"),
            type,
            start = "08:00",
            end = "16:00",
            description = "UI test"
        });
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
