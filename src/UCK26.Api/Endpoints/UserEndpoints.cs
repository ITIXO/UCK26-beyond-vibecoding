using Microsoft.EntityFrameworkCore;
using UCK26.Api.Auth;
using UCK26.Api.Persistence;

namespace UCK26.Api.Endpoints;

public record UserDto(int Id, string UserName, string Role, DateTime CreatedAt);
public record CreateUserRequest(string UserName, string Password, string Role);
public record UpdateUserRequest(string? UserName, string? Role, string? Password);

public static class UserEndpoints
{
    public static IEndpointRouteBuilder MapUsers(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/users")
            .WithTags("Users")
            .RequireAuthorization("Admin");

        group.MapGet("/", async (AppDbContext db, CancellationToken ct) =>
        {
            var users = await db.Users
                .OrderBy(u => u.UserName)
                .Select(u => new UserDto(u.Id, u.UserName, u.Role, u.CreatedAt))
                .ToListAsync(ct);
            return Results.Ok(users);
        });

        group.MapPost("/", async (CreateUserRequest body, AppDbContext db, IPasswordHasher hasher, CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(body.UserName) || string.IsNullOrWhiteSpace(body.Password))
            {
                return Results.BadRequest(new { error = "UserName and Password are required." });
            }

            if (body.Role is not (UserRoles.Admin or UserRoles.User))
            {
                return Results.BadRequest(new { error = $"Role must be '{UserRoles.Admin}' or '{UserRoles.User}'." });
            }

            if (await db.Users.AnyAsync(u => u.UserName == body.UserName, ct))
            {
                return Results.Conflict(new { error = "User name already exists." });
            }

            var user = new User
            {
                UserName = body.UserName,
                PasswordHash = hasher.Hash(body.Password),
                Role = body.Role
            };
            db.Users.Add(user);
            await db.SaveChangesAsync(ct);

            return Results.Created(
                $"/api/users/{user.Id}",
                new UserDto(user.Id, user.UserName, user.Role, user.CreatedAt));
        });

        group.MapPut("/{id:int}", async (int id, UpdateUserRequest body, AppDbContext db, IPasswordHasher hasher, CancellationToken ct) =>
        {
            var user = await db.Users.FirstOrDefaultAsync(u => u.Id == id, ct);
            if (user is null)
            {
                return Results.NotFound();
            }

            if (!string.IsNullOrWhiteSpace(body.UserName) && body.UserName != user.UserName)
            {
                if (await db.Users.AnyAsync(u => u.UserName == body.UserName, ct))
                {
                    return Results.Conflict(new { error = "User name already exists." });
                }
                user.UserName = body.UserName;
            }

            if (!string.IsNullOrWhiteSpace(body.Role))
            {
                if (body.Role is not (UserRoles.Admin or UserRoles.User))
                {
                    return Results.BadRequest(new { error = $"Role must be '{UserRoles.Admin}' or '{UserRoles.User}'." });
                }
                user.Role = body.Role;
            }

            if (!string.IsNullOrWhiteSpace(body.Password))
            {
                user.PasswordHash = hasher.Hash(body.Password);
            }

            await db.SaveChangesAsync(ct);
            return Results.Ok(new UserDto(user.Id, user.UserName, user.Role, user.CreatedAt));
        });

        group.MapDelete("/{id:int}", async (int id, AppDbContext db, CancellationToken ct) =>
        {
            var user = await db.Users.FirstOrDefaultAsync(u => u.Id == id, ct);
            if (user is null)
            {
                return Results.NotFound();
            }

            db.Users.Remove(user);
            await db.SaveChangesAsync(ct);
            return Results.NoContent();
        });

        return app;
    }
}
