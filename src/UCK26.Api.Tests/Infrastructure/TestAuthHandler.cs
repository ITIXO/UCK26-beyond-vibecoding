using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace UCK26.Api.Tests.Infrastructure;

public sealed class TestAuthHandler(
    IOptionsMonitor<TestAuthOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder) : AuthenticationHandler<TestAuthOptions>(options, logger, encoder)
{
    public const string SchemeName = "TestAuth";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var state = Context.RequestServices.GetRequiredService<TestAuthState>();
        var snapshot = state.GetSnapshot();
        if (!snapshot.Authenticate)
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var identity = new ClaimsIdentity(snapshot.Claims, SchemeName, ClaimTypes.Name, ClaimTypes.Role);
        var ticket = new AuthenticationTicket(new ClaimsPrincipal(identity), SchemeName);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}

public sealed class TestAuthOptions : AuthenticationSchemeOptions;

public sealed class TestAuthState
{
    private readonly Lock _sync = new();
    private bool _authenticate;
    private List<Claim> _claims = [];

    public void SetAuthenticated(IEnumerable<Claim> claims)
    {
        lock (_sync)
        {
            _authenticate = true;
            _claims = [.. claims];
        }
    }

    public void SetUnauthenticated()
    {
        lock (_sync)
        {
            _authenticate = false;
            _claims = [];
        }
    }

    public TestAuthSnapshot GetSnapshot()
    {
        lock (_sync)
        {
            return new TestAuthSnapshot(_authenticate, [.. _claims]);
        }
    }
}

public sealed record TestAuthSnapshot(bool Authenticate, List<Claim> Claims);
