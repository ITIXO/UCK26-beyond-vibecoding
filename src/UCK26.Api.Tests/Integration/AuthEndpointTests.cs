using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using UCK26.Api.Tests.Infrastructure;

namespace UCK26.Api.Tests.Integration;

public class AuthEndpointTests
{
    [Test]
    public async Task Login_ValidCredentials_Returns200WithToken()
    {
        await using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/login", new
        {
            userName = "admin",
            password = "Demo!2026"
        });

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        await Assert.That(body.GetProperty("accessToken").GetString()).IsNotNull();
        await Assert.That(body.GetProperty("user").GetProperty("userName").GetString()).IsEqualTo("admin");
        await Assert.That(body.GetProperty("user").GetProperty("role").GetString()).IsEqualTo("Admin");
    }

    [Test]
    public async Task Login_WrongPassword_Returns401()
    {
        await using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await client.PostAsJsonAsync("/api/auth/login", new
        {
            userName = "admin",
            password = "wrong-password"
        });

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task Login_UnknownUser_Returns401()
    {
        await using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await client.PostAsJsonAsync("/api/auth/login", new
        {
            userName = "ghost",
            password = "anything"
        });

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task Login_EmptyBody_Returns400()
    {
        await using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await client.PostAsJsonAsync("/api/auth/login", new { userName = "", password = "" });

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task Me_Unauthenticated_Returns401()
    {
        await using var factory = new TestWebApplicationFactory().Unauthenticated();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await client.GetAsync("/api/auth/me");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task Me_Authenticated_ReturnsUserClaims()
    {
        await using var factory = new TestWebApplicationFactory()
            .AuthenticateAs("alice", 42, "User");
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/auth/me");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        await Assert.That(body.GetProperty("id").GetInt32()).IsEqualTo(42);
        await Assert.That(body.GetProperty("userName").GetString()).IsEqualTo("alice");
        await Assert.That(body.GetProperty("role").GetString()).IsEqualTo("User");
    }
}
