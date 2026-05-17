using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using UCK26.Api.Auth;
using UCK26.Api.Persistence;

namespace UCK26.Api.Endpoints;

public record LoginRequest(string UserName, string Password);
public record LoginResponse(string AccessToken, int ExpiresInSeconds, MeResponse User);
public record MeResponse(int Id, string UserName, string Role);

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuth(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth").WithTags("Auth");

        group.MapPost("/login", async (
            LoginRequest body,
            AppDbContext db,
            IPasswordHasher hasher,
            IJwtTokenService jwt,
            Microsoft.Extensions.Options.IOptions<JwtOptions> jwtOptions,
            CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(body.UserName) || string.IsNullOrWhiteSpace(body.Password))
            {
                return Results.BadRequest(new { error = "UserName and Password are required." });
            }

            var user = await db.Users.FirstOrDefaultAsync(u => u.UserName == body.UserName, ct);
            if (user is null || !hasher.Verify(body.Password, user.PasswordHash))
            {
                return Results.Unauthorized();
            }

            var token = jwt.Issue(user);
            var response = new LoginResponse(
                token,
                jwtOptions.Value.ExpiresMinutes * 60,
                new MeResponse(user.Id, user.UserName, user.Role));
            return Results.Ok(response);
        })
        .AllowAnonymous()
        .WithName("Login");

        group.MapGet("/me", (ClaimsPrincipal principal) =>
        {
            var idClaim = principal.FindFirstValue(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)
                ?? principal.FindFirstValue(ClaimTypes.NameIdentifier);
            var name = principal.FindFirstValue(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Name)
                ?? principal.Identity?.Name;
            var role = principal.FindFirstValue(ClaimTypes.Role) ?? "";

            if (idClaim is null || name is null)
            {
                return Results.Unauthorized();
            }

            return Results.Ok(new MeResponse(int.Parse(idClaim), name, role));
        })
        .RequireAuthorization()
        .WithName("Me");

        return app;
    }
}
