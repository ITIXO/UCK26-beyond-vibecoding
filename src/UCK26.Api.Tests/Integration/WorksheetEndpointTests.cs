using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using UCK26.Api.Tests.Infrastructure;

namespace UCK26.Api.Tests.Integration;

public class WorksheetEndpointTests
{
    [Test]
    public async Task Get_Unauthenticated_Returns401()
    {
        await using var factory = new TestWebApplicationFactory().Unauthenticated();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await client.GetAsync("/api/worksheets?year=2026&month=5");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task Get_AsUser_ReturnsWorksheetForMonth()
    {
        await using var factory = new TestWebApplicationFactory()
            .AuthenticateAs("alice", 2, "User");
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/worksheets?year=2026&month=5");
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);

        var worksheet = await response.Content.ReadFromJsonAsync<JsonElement>();
        await Assert.That(worksheet.GetProperty("userName").GetString()).IsEqualTo("alice");
        await Assert.That(worksheet.GetProperty("year").GetInt32()).IsEqualTo(2026);
        await Assert.That(worksheet.GetProperty("month").GetInt32()).IsEqualTo(5);
        await Assert.That(worksheet.GetProperty("entries").GetArrayLength()).IsEqualTo(0);
    }

    [Test]
    public async Task PutEntry_AsUser_UpsertsEntry()
    {
        await using var factory = new TestWebApplicationFactory()
            .AuthenticateAs("alice", 2, "User");
        using var client = factory.CreateClient();

        var response = await client.PutAsJsonAsync("/api/worksheets/entries", new
        {
            date = "2026-05-17",
            type = "work",
            hours = 7.5m
        });
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);

        var worksheetResponse = await client.GetAsync("/api/worksheets?year=2026&month=5");
        var worksheet = await worksheetResponse.Content.ReadFromJsonAsync<JsonElement>();
        var entry = worksheet.GetProperty("entries").EnumerateArray().Single();

        await Assert.That(entry.GetProperty("date").GetString()).IsEqualTo("2026-05-17");
        await Assert.That(entry.GetProperty("type").GetString()).IsEqualTo("work");
        await Assert.That(entry.GetProperty("hours").GetDecimal()).IsEqualTo(7.5m);
    }

    [Test]
    public async Task PutEntry_ZeroHours_RemovesEntry()
    {
        await using var factory = new TestWebApplicationFactory()
            .AuthenticateAs("alice", 2, "User");
        using var client = factory.CreateClient();

        await client.PutAsJsonAsync("/api/worksheets/entries", new
        {
            date = "2026-05-17",
            type = "doctor",
            hours = 2m
        });

        var delete = await client.PutAsJsonAsync("/api/worksheets/entries", new
        {
            date = "2026-05-17",
            type = "doctor",
            hours = 0m
        });
        await Assert.That(delete.StatusCode).IsEqualTo(HttpStatusCode.NoContent);

        var worksheetResponse = await client.GetAsync("/api/worksheets?year=2026&month=5");
        var worksheet = await worksheetResponse.Content.ReadFromJsonAsync<JsonElement>();
        await Assert.That(worksheet.GetProperty("entries").GetArrayLength()).IsEqualTo(0);
    }
}
