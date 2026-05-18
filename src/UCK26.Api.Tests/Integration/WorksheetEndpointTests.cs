using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using UCK26.Api.Persistence;
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
    public async Task Get_AsAdmin_WithUserId_ReturnsSelectedUserWorksheet()
    {
        await using var factory = new TestWebApplicationFactory()
            .AuthenticateAs("admin", 1, "Admin");
        using var client = factory.CreateClient();

        var createUser = await client.PostAsJsonAsync("/api/users", new
        {
            userName = "worker",
            password = "Worker!2026",
            role = "User"
        });
        var createdUser = await createUser.Content.ReadFromJsonAsync<JsonElement>();
        var userId = createdUser.GetProperty("id").GetInt32();

        var response = await client.GetAsync($"/api/worksheets?year=2026&month=5&userId={userId}");
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);

        var worksheet = await response.Content.ReadFromJsonAsync<JsonElement>();
        await Assert.That(worksheet.GetProperty("userId").GetInt32()).IsEqualTo(userId);
        await Assert.That(worksheet.GetProperty("userName").GetString()).IsEqualTo("worker");
    }

    [Test]
    public async Task PostEntry_AsUser_CreatesEntry()
    {
        await using var factory = new TestWebApplicationFactory()
            .AuthenticateAs("alice", 2, "User");
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/worksheets/entries", new
        {
            date = "2026-05-17",
            type = "work",
            start = "08:00",
            end = "15:30",
            description = "Feature work"
        });
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Created);

        var worksheetResponse = await client.GetAsync("/api/worksheets?year=2026&month=5");
        var worksheet = await worksheetResponse.Content.ReadFromJsonAsync<JsonElement>();
        var entry = worksheet.GetProperty("entries").EnumerateArray().Single();

        await Assert.That(entry.GetProperty("date").GetString()).IsEqualTo("2026-05-17");
        await Assert.That(entry.GetProperty("type").GetString()).IsEqualTo("work");
        await Assert.That(entry.GetProperty("start").GetString()).IsEqualTo("08:00:00");
        await Assert.That(entry.GetProperty("end").GetString()).IsEqualTo("15:30:00");
        await Assert.That(entry.GetProperty("description").GetString()).IsEqualTo("Feature work");
        await Assert.That(entry.GetProperty("hours").GetDecimal()).IsEqualTo(7.5m);
    }

    [Test]
    public async Task PostEntry_AllowsMultipleEntriesPerDateAndType()
    {
        await using var factory = new TestWebApplicationFactory()
            .AuthenticateAs("alice", 2, "User");
        using var client = factory.CreateClient();

        await client.PostAsJsonAsync("/api/worksheets/entries", new
        {
            date = "2026-05-17",
            type = "work",
            start = "08:00",
            end = "10:00",
            description = "Morning"
        });

        await client.PostAsJsonAsync("/api/worksheets/entries", new
        {
            date = "2026-05-17",
            type = "work",
            start = "11:00",
            end = "12:00",
            description = "Noon"
        });

        var worksheetResponse = await client.GetAsync("/api/worksheets?year=2026&month=5");
        var worksheet = await worksheetResponse.Content.ReadFromJsonAsync<JsonElement>();
        await Assert.That(worksheet.GetProperty("entries").GetArrayLength()).IsEqualTo(2);
    }

    [Test]
    public async Task PutEntry_AsOwner_UpdatesEntry()
    {
        await using var factory = new TestWebApplicationFactory()
            .AuthenticateAs("alice", 2, "User");
        using var client = factory.CreateClient();

        var create = await client.PostAsJsonAsync("/api/worksheets/entries", new
        {
            date = "2026-05-17",
            type = "doctor",
            start = "09:00",
            end = "10:00",
            description = "Checkup"
        });
        var created = await create.Content.ReadFromJsonAsync<JsonElement>();
        var id = created.GetProperty("id").GetInt32();

        var update = await client.PutAsJsonAsync($"/api/worksheets/entries/{id}", new
        {
            start = "09:30",
            end = "11:00",
            description = "Specialist"
        });
        await Assert.That(update.StatusCode).IsEqualTo(HttpStatusCode.OK);

        var updated = await update.Content.ReadFromJsonAsync<JsonElement>();
        await Assert.That(updated.GetProperty("start").GetString()).IsEqualTo("09:30:00");
        await Assert.That(updated.GetProperty("end").GetString()).IsEqualTo("11:00:00");
        await Assert.That(updated.GetProperty("description").GetString()).IsEqualTo("Specialist");
        await Assert.That(updated.GetProperty("hours").GetDecimal()).IsEqualTo(1.5m);
    }

    [Test]
    public async Task DeleteEntry_AsOwner_RemovesEntry()
    {
        await using var factory = new TestWebApplicationFactory()
            .AuthenticateAs("alice", 2, "User");
        using var client = factory.CreateClient();

        var create = await client.PostAsJsonAsync("/api/worksheets/entries", new
        {
            date = "2026-05-17",
            type = "holiday",
            start = "08:00",
            end = "16:00",
            description = "Vacation"
        });
        var created = await create.Content.ReadFromJsonAsync<JsonElement>();
        var id = created.GetProperty("id").GetInt32();

        var delete = await client.DeleteAsync($"/api/worksheets/entries/{id}");
        await Assert.That(delete.StatusCode).IsEqualTo(HttpStatusCode.NoContent);

        var worksheetResponse = await client.GetAsync("/api/worksheets?year=2026&month=5");
        var worksheet = await worksheetResponse.Content.ReadFromJsonAsync<JsonElement>();
        await Assert.That(worksheet.GetProperty("entries").GetArrayLength()).IsEqualTo(0);
    }

    [Test]
    public async Task PostEntry_WritesCreateAuditLog()
    {
        await using var factory = new TestWebApplicationFactory()
            .AuthenticateAs("alice", 2, "User");
        using var client = factory.CreateClient();

        await CreateEntryAsync(client, "2026-05-17", "work", "08:00", "15:30", "Feature work");

        var log = ReadAuditLogs(factory).Single();
        await Assert.That(log.Action).IsEqualTo(AuditAction.Create);
        await Assert.That(log.PerformedBy).IsEqualTo("alice");

        var fields = DeserializeFields(log.ChangedFields);
        await Assert.That(fields.Select(f => f.Field)).Contains(nameof(WorkEntry.WorksheetId));
        await Assert.That(fields.Select(f => f.Field)).Contains(nameof(WorkEntry.Date));
        await Assert.That(fields.Select(f => f.Field)).Contains(nameof(WorkEntry.Type));
        await Assert.That(fields.Select(f => f.Field)).Contains(nameof(WorkEntry.Start));
        await Assert.That(fields.Select(f => f.Field)).Contains(nameof(WorkEntry.End));
        await Assert.That(fields.Select(f => f.Field)).Contains(nameof(WorkEntry.Description));
        await Assert.That(fields.All(f => f.OldValue is null)).IsTrue();
    }

    [Test]
    public async Task PutEntry_WritesUpdateAuditLogWithChangedFields()
    {
        await using var factory = new TestWebApplicationFactory()
            .AuthenticateAs("alice", 2, "User");
        using var client = factory.CreateClient();

        var created = await CreateEntryAsync(client, "2026-05-17", "doctor", "09:00", "10:00", "Checkup");
        var id = created.GetProperty("id").GetInt32();

        var update = await client.PutAsJsonAsync($"/api/worksheets/entries/{id}", new
        {
            start = "09:30",
            end = "11:00",
            description = "Specialist"
        });
        await Assert.That(update.StatusCode).IsEqualTo(HttpStatusCode.OK);

        var log = ReadAuditLogs(factory).OrderBy(log => log.Id).Last();
        await Assert.That(log.Action).IsEqualTo(AuditAction.Update);

        var fields = DeserializeFields(log.ChangedFields);
        await Assert.That(fields.Select(f => f.Field)).IsEquivalentTo([nameof(WorkEntry.Start), nameof(WorkEntry.End), nameof(WorkEntry.Description)]);
        var end = fields.Single(f => f.Field == nameof(WorkEntry.End));
        await Assert.That(end.OldValue).IsEqualTo("10:00:00");
        await Assert.That(end.NewValue).IsEqualTo("11:00:00");
    }

    [Test]
    public async Task DeleteEntry_WritesDeleteAuditLogWithDenormalizedFields()
    {
        await using var factory = new TestWebApplicationFactory()
            .AuthenticateAs("alice", 2, "User");
        using var client = factory.CreateClient();

        var created = await CreateEntryAsync(client, "2026-05-17", "holiday", "08:00", "16:00", "Vacation");
        var id = created.GetProperty("id").GetInt32();
        var worksheetId = await GetWorksheetIdAsync(client);

        var delete = await client.DeleteAsync($"/api/worksheets/entries/{id}");
        await Assert.That(delete.StatusCode).IsEqualTo(HttpStatusCode.NoContent);

        var log = ReadAuditLogs(factory).OrderBy(log => log.Id).Last();
        await Assert.That(log.Action).IsEqualTo(AuditAction.Delete);
        await Assert.That(log.ChangedFields).IsNull();
        await Assert.That(log.EntryDate).IsEqualTo("2026-05-17");
        await Assert.That(log.WorksheetId).IsEqualTo(worksheetId);
    }

    [Test]
    public async Task GetAudit_WithNoWorksheet_ReturnsEmptyArray()
    {
        await using var factory = new TestWebApplicationFactory()
            .AuthenticateAs("alice", 2, "User");
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/worksheets/2026/5/days/2026-05-17/audit");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        var audit = await response.Content.ReadFromJsonAsync<JsonElement>();
        await Assert.That(audit.GetArrayLength()).IsEqualTo(0);
    }

    [Test]
    public async Task GetAudit_ReturnsEventsDescendingWithChangedFields()
    {
        await using var factory = new TestWebApplicationFactory()
            .AuthenticateAs("alice", 2, "User");
        using var client = factory.CreateClient();

        var created = await CreateEntryAsync(client, "2026-05-17", "work", "08:00", "10:00", "Morning");
        var id = created.GetProperty("id").GetInt32();
        await client.PutAsJsonAsync($"/api/worksheets/entries/{id}", new
        {
            start = "08:00",
            end = "11:00",
            description = "Long morning"
        });

        var response = await client.GetAsync("/api/worksheets/2026/5/days/2026-05-17/audit");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        var audit = await response.Content.ReadFromJsonAsync<JsonElement>();
        await Assert.That(audit.GetArrayLength()).IsEqualTo(2);
        await Assert.That(audit[0].GetProperty("action").GetString()).IsEqualTo("Update");
        await Assert.That(audit[1].GetProperty("action").GetString()).IsEqualTo("Create");
        await Assert.That(audit[0].GetProperty("changedFields").EnumerateArray()
            .Any(field => field.GetProperty("field").GetString() == nameof(WorkEntry.End))).IsTrue();
    }

    [Test]
    public async Task GetAudit_AsNonAdminWithUserId_ReturnsOwnData()
    {
        await using var factory = new TestWebApplicationFactory()
            .AuthenticateAs("alice", 2, "User");
        using var client = factory.CreateClient();

        await CreateEntryAsync(client, "2026-05-17", "work", "08:00", "10:00", "Own");

        var response = await client.GetAsync("/api/worksheets/2026/5/days/2026-05-17/audit?userId=99");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        var audit = await response.Content.ReadFromJsonAsync<JsonElement>();
        await Assert.That(audit.GetArrayLength()).IsEqualTo(1);
        await Assert.That(audit[0].GetProperty("performedBy").GetString()).IsEqualTo("alice");
    }

    [Test]
    public async Task GetAudit_AsAdminWithUserId_ReturnsSelectedUserData()
    {
        await using var factory = new TestWebApplicationFactory()
            .AuthenticateAs("admin", 1, "Admin");
        using var client = factory.CreateClient();

        var createUser = await client.PostAsJsonAsync("/api/users", new
        {
            userName = "worker",
            password = "Worker!2026",
            role = "User"
        });
        var createdUser = await createUser.Content.ReadFromJsonAsync<JsonElement>();
        var userId = createdUser.GetProperty("id").GetInt32();

        await CreateEntryAsync(client, "2026-05-17", "work", "08:00", "10:00", "Worker", userId);

        var response = await client.GetAsync($"/api/worksheets/2026/5/days/2026-05-17/audit?userId={userId}");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        var audit = await response.Content.ReadFromJsonAsync<JsonElement>();
        await Assert.That(audit.GetArrayLength()).IsEqualTo(1);
        await Assert.That(audit[0].GetProperty("performedBy").GetString()).IsEqualTo("admin");
    }

    private static async Task<JsonElement> CreateEntryAsync(
        HttpClient client,
        string date,
        string type,
        string start,
        string end,
        string description,
        int? userId = null)
    {
        var response = await client.PostAsJsonAsync("/api/worksheets/entries", new
        {
            date,
            type,
            start,
            end,
            description,
            userId
        });
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Created);
        return await response.Content.ReadFromJsonAsync<JsonElement>();
    }

    private static async Task<int> GetWorksheetIdAsync(HttpClient client)
    {
        var worksheetResponse = await client.GetAsync("/api/worksheets?year=2026&month=5");
        var worksheet = await worksheetResponse.Content.ReadFromJsonAsync<JsonElement>();
        return worksheet.GetProperty("id").GetInt32();
    }

    private static List<AuditLog> ReadAuditLogs(TestWebApplicationFactory factory)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return db.AuditLogs.OrderBy(log => log.Id).ToList();
    }

    private static List<AuditChangedField> DeserializeFields(string? value) =>
        JsonSerializer.Deserialize<List<AuditChangedField>>(value!, new JsonSerializerOptions(JsonSerializerDefaults.Web))!;
}
