using Microsoft.Extensions.Options;
using UCK26.Api.Auth;

namespace UCK26.Api.Persistence;

public class SeedOptions
{
    public const string SectionName = "Seed";
    public string AdminUserName { get; set; } = "admin";
    public string AdminPassword { get; set; } = "admin";
}

public static class DbSeeder
{
    public static async Task SeedAsync(
        AppDbContext db,
        IPasswordHasher hasher,
        IOptions<SeedOptions> seedOptions,
        CancellationToken ct = default)
    {
        await db.Database.EnsureCreatedAsync(ct);

        var seed = seedOptions.Value;
        if (db.Users.Any(u => u.UserName == seed.AdminUserName))
        {
            return;
        }

        db.Users.Add(new User
        {
            UserName = seed.AdminUserName,
            PasswordHash = hasher.Hash(seed.AdminPassword),
            Role = UserRoles.Admin
        });

        await db.SaveChangesAsync(ct);
    }
}
