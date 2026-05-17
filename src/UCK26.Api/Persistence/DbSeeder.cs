using Microsoft.EntityFrameworkCore;
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
        await EnsureWorksheetSchemaAsync(db, ct);

        var seed = seedOptions.Value;
        var admin = db.Users.FirstOrDefault(u => u.UserName == seed.AdminUserName);
        if (admin is not null)
        {
            if (admin.Role != UserRoles.Admin)
            {
                admin.Role = UserRoles.Admin;
                await db.SaveChangesAsync(ct);
            }

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

    private static async Task EnsureWorksheetSchemaAsync(AppDbContext db, CancellationToken ct)
    {
        await db.Database.ExecuteSqlRawAsync("""
            CREATE TABLE IF NOT EXISTS "Worksheets" (
                "Id" INTEGER NOT NULL CONSTRAINT "PK_Worksheets" PRIMARY KEY AUTOINCREMENT,
                "UserId" INTEGER NOT NULL,
                "UserName" TEXT NOT NULL,
                "Year" INTEGER NOT NULL,
                "Month" INTEGER NOT NULL
            );
            """, ct);

        await db.Database.ExecuteSqlRawAsync("""
            CREATE UNIQUE INDEX IF NOT EXISTS "IX_Worksheets_UserId_Year_Month"
            ON "Worksheets" ("UserId", "Year", "Month");
            """, ct);

        await db.Database.ExecuteSqlRawAsync("""
            CREATE TABLE IF NOT EXISTS "WorkEntries" (
                "Id" INTEGER NOT NULL CONSTRAINT "PK_WorkEntries" PRIMARY KEY AUTOINCREMENT,
                "WorksheetId" INTEGER NOT NULL,
                "Date" TEXT NOT NULL,
                "Type" TEXT NOT NULL,
                "Hours" TEXT NOT NULL,
                CONSTRAINT "FK_WorkEntries_Worksheets_WorksheetId"
                    FOREIGN KEY ("WorksheetId") REFERENCES "Worksheets" ("Id") ON DELETE CASCADE
            );
            """, ct);

        await db.Database.ExecuteSqlRawAsync("""
            CREATE UNIQUE INDEX IF NOT EXISTS "IX_WorkEntries_WorksheetId_Date_Type"
            ON "WorkEntries" ("WorksheetId", "Date", "Type");
            """, ct);
    }
}
