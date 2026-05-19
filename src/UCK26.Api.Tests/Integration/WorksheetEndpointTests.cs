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

    // Audit interceptor tests (#3)

    [Test]
    public async Task Audit_PostEntry_CreatesAuditLogWithCreateAction()
    {
        await using var factory = new TestWebApplicationFactory()
            .AuthenticateAs("alice", 2, "User");
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/worksheets/entries", new
        {
            date = "2026-05-17",
            type = "work",
            start = "08:00",
            end = "16:00",
            description = "Day"
        });
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Created);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var logs = db.AuditLogs.ToList();

        await Assert.That(logs.Count).IsEqualTo(1);
        await Assert.That(logs[0].Action).IsEqualTo(AuditAction.Create);
        await Assert.That(logs[0].PerformedBy).IsEqualTo("alice");
        await Assert.That(logs[0].EntryType).IsEqualTo("work");
        await Assert.That(logs[0].EntryDate).IsEqualTo("2026-05-17");
        await Assert.That(logs[0].ChangedFields).IsNotNull();
    }

    [Test]
    public async Task Audit_PutEntry_CreatesUpdateLogWithChangedFields()
    {
        await using var factory = new TestWebApplicationFactory()
            .AuthenticateAs("alice", 2, "User");
        using var client = factory.CreateClient();

        var create = await client.PostAsJsonAsync("/api/worksheets/entries", new
        {
            date = "2026-05-17",
            type = "work",
            start = "08:00",
            end = "16:00",
            description = "Before"
        });
        var created = await create.Content.ReadFromJsonAsync<JsonElement>();
        var id = created.GetProperty("id").GetInt32();

        await client.PutAsJsonAsync($"/api/worksheets/entries/{id}", new
        {
            start = "09:00",
            end = "17:00",
            description = "After"
        });

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var updateLog = db.AuditLogs.Single(l => l.Action == AuditAction.Update);

        await Assert.That(updateLog.PerformedBy).IsEqualTo("alice");
        await Assert.That(updateLog.EntryType).IsEqualTo("work");
        await Assert.That(updateLog.ChangedFields).IsNotNull();

        var fields = JsonSerializer.Deserialize<List<AuditChangedField>>(updateLog.ChangedFields!);
        await Assert.That(fields).IsNotNull();
        await Assert.That(fields!.Any(f => f.Field == "Description" && f.OldValue == "Before" && f.NewValue == "After")).IsTrue();
        await Assert.That(fields!.Any(f => f.Field == "Start")).IsTrue();
        await Assert.That(fields!.Any(f => f.Field == "End")).IsTrue();
    }

    [Test]
    public async Task Audit_DeleteEntry_PreservesLogAfterDeletion()
    {
        await using var factory = new TestWebApplicationFactory()
            .AuthenticateAs("alice", 2, "User");
        using var client = factory.CreateClient();

        var create = await client.PostAsJsonAsync("/api/worksheets/entries", new
        {
            date = "2026-05-20",
            type = "holiday",
            start = "08:00",
            end = "16:00",
            description = "Day off"
        });
        var created = await create.Content.ReadFromJsonAsync<JsonElement>();
        var id = created.GetProperty("id").GetInt32();

        await client.DeleteAsync($"/api/worksheets/entries/{id}");

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var deleteLog = db.AuditLogs.Single(l => l.Action == AuditAction.Delete);

        await Assert.That(deleteLog.EntryDate).IsEqualTo("2026-05-20");
        await Assert.That(deleteLog.EntryType).IsEqualTo("holiday");
        await Assert.That(deleteLog.WorksheetId).IsNotNull();
        await Assert.That(deleteLog.ChangedFields).IsNotNull();
        await Assert.That(db.WorkEntries.Any(e => e.Id == id)).IsFalse();
    }

    // Audit endpoint tests (#4)

    [Test]
    public async Task GetAudit_NoWorksheet_Returns200EmptyArray()
    {
        await using var factory = new TestWebApplicationFactory()
            .AuthenticateAs("alice", 2, "User");
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/worksheets/2026/5/days/2026-05-17/audit");
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);

        var logs = await response.Content.ReadFromJsonAsync<JsonElement>();
        await Assert.That(logs.GetArrayLength()).IsEqualTo(0);
    }

    [Test]
    public async Task GetAudit_AfterPostEntry_ReturnsCreateEvent()
    {
        await using var factory = new TestWebApplicationFactory()
            .AuthenticateAs("alice", 2, "User");
        using var client = factory.CreateClient();

        await client.PostAsJsonAsync("/api/worksheets/entries", new
        {
            date = "2026-05-17",
            type = "work",
            start = "08:00",
            end = "16:00",
            description = "Day"
        });

        var response = await client.GetAsync("/api/worksheets/2026/5/days/2026-05-17/audit");
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);

        var logs = await response.Content.ReadFromJsonAsync<JsonElement>();
        await Assert.That(logs.GetArrayLength()).IsEqualTo(1);
        await Assert.That(logs[0].GetProperty("action").GetString()).IsEqualTo("Create");
        await Assert.That(logs[0].GetProperty("entryType").GetString()).IsEqualTo("work");
    }

    [Test]
    public async Task GetAudit_AfterPostAndPut_ReturnsTwoEventsDescending()
    {
        await using var factory = new TestWebApplicationFactory()
            .AuthenticateAs("alice", 2, "User");
        using var client = factory.CreateClient();

        var create = await client.PostAsJsonAsync("/api/worksheets/entries", new
        {
            date = "2026-05-17",
            type = "work",
            start = "08:00",
            end = "16:00",
            description = "Day"
        });
        var created = await create.Content.ReadFromJsonAsync<JsonElement>();
        var id = created.GetProperty("id").GetInt32();

        await client.PutAsJsonAsync($"/api/worksheets/entries/{id}", new
        {
            start = "09:00",
            end = "17:00",
            description = "Updated"
        });

        var response = await client.GetAsync("/api/worksheets/2026/5/days/2026-05-17/audit");
        var logs = await response.Content.ReadFromJsonAsync<JsonElement>();

        await Assert.That(logs.GetArrayLength()).IsEqualTo(2);
        await Assert.That(logs[0].GetProperty("action").GetString()).IsEqualTo("Update");
        await Assert.That(logs[0].GetProperty("entryType").GetString()).IsEqualTo("work");
        await Assert.That(logs[0].GetProperty("changedFields").GetArrayLength()).IsGreaterThan(0);
        await Assert.That(logs[1].GetProperty("action").GetString()).IsEqualTo("Create");
    }

    [Test]
    public async Task GetAudit_NonAdmin_WithOtherUserId_ReturnsOwnData()
    {
        await using var factory = new TestWebApplicationFactory()
            .AuthenticateAs("alice", 2, "User");
        using var client = factory.CreateClient();

        await client.PostAsJsonAsync("/api/worksheets/entries", new
        {
            date = "2026-05-17",
            type = "work",
            start = "08:00",
            end = "16:00",
            description = "Mine"
        });

        var response = await client.GetAsync("/api/worksheets/2026/5/days/2026-05-17/audit?userId=99");
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);

        var logs = await response.Content.ReadFromJsonAsync<JsonElement>();
        await Assert.That(logs.GetArrayLength()).IsEqualTo(1);
    }

    [Test]
    public async Task GetAudit_Admin_WithUserId_ReturnsTargetUserData()
    {
        await using var factory = new TestWebApplicationFactory()
            .AuthenticateAs("admin", 1, "Admin");
        using var client = factory.CreateClient();

        var createUser = await client.PostAsJsonAsync("/api/users", new
        {
            userName = "worker2",
            password = "Worker!2026",
            role = "User"
        });
        var createdUser = await createUser.Content.ReadFromJsonAsync<JsonElement>();
        var userId = createdUser.GetProperty("id").GetInt32();

        await client.PostAsJsonAsync("/api/worksheets/entries", new
        {
            date = "2026-05-17",
            type = "work",
            start = "08:00",
            end = "16:00",
            description = "Admin entry",
            userId = userId
        });

        var response = await client.GetAsync($"/api/worksheets/2026/5/days/2026-05-17/audit?userId={userId}");
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);

        var logs = await response.Content.ReadFromJsonAsync<JsonElement>();
        await Assert.That(logs.GetArrayLength()).IsEqualTo(1);
    }
}
