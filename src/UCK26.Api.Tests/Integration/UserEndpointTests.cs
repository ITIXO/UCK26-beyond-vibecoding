using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using UCK26.Api.Tests.Infrastructure;

namespace UCK26.Api.Tests.Integration;

public class UserEndpointTests
{
    [Test]
    public async Task List_AsAdmin_Returns200WithSeededAdmin()
    {
        await using var factory = new TestWebApplicationFactory()
            .AuthenticateAs("admin", 1, "Admin");
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/users");
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);

        var users = await response.Content.ReadFromJsonAsync<JsonElement>();
        await Assert.That(users.GetArrayLength()).IsGreaterThanOrEqualTo(1);
        var hasAdmin = users.EnumerateArray()
            .Any(u => u.GetProperty("userName").GetString() == "admin");
        await Assert.That(hasAdmin).IsTrue();
    }

    [Test]
    public async Task List_AsUser_Returns403()
    {
        await using var factory = new TestWebApplicationFactory()
            .AuthenticateAs("alice", 2, "User");
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await client.GetAsync("/api/users");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task List_Unauthenticated_Returns401()
    {
        await using var factory = new TestWebApplicationFactory().Unauthenticated();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await client.GetAsync("/api/users");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task Create_AsAdmin_Returns201AndAppearsInList()
    {
        await using var factory = new TestWebApplicationFactory()
            .AuthenticateAs("admin", 1, "Admin");
        using var client = factory.CreateClient();

        var create = await client.PostAsJsonAsync("/api/users", new
        {
            userName = "bob",
            password = "Bob!2026",
            role = "User"
        });
        await Assert.That(create.StatusCode).IsEqualTo(HttpStatusCode.Created);

        var list = await client.GetAsync("/api/users");
        var users = await list.Content.ReadFromJsonAsync<JsonElement>();
        var hasBob = users.EnumerateArray().Any(u => u.GetProperty("userName").GetString() == "bob");
        await Assert.That(hasBob).IsTrue();
    }

    [Test]
    public async Task Create_DuplicateUserName_Returns409()
    {
        await using var factory = new TestWebApplicationFactory()
            .AuthenticateAs("admin", 1, "Admin");
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/users", new
        {
            userName = "admin",
            password = "x",
            role = "User"
        });

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Conflict);
    }

    [Test]
    public async Task Create_InvalidRole_Returns400()
    {
        await using var factory = new TestWebApplicationFactory()
            .AuthenticateAs("admin", 1, "Admin");
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/users", new
        {
            userName = "carol",
            password = "x",
            role = "Superhero"
        });

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task Delete_AsAdmin_RemovesUser()
    {
        await using var factory = new TestWebApplicationFactory()
            .AuthenticateAs("admin", 1, "Admin");
        using var client = factory.CreateClient();

        var create = await client.PostAsJsonAsync("/api/users", new
        {
            userName = "dave",
            password = "x",
            role = "User"
        });
        var created = await create.Content.ReadFromJsonAsync<JsonElement>();
        var id = created.GetProperty("id").GetInt32();

        var delete = await client.DeleteAsync($"/api/users/{id}");
        await Assert.That(delete.StatusCode).IsEqualTo(HttpStatusCode.NoContent);

        var list = await client.GetAsync("/api/users");
        var users = await list.Content.ReadFromJsonAsync<JsonElement>();
        var stillThere = users.EnumerateArray().Any(u => u.GetProperty("userName").GetString() == "dave");
        await Assert.That(stillThere).IsFalse();
    }
}
