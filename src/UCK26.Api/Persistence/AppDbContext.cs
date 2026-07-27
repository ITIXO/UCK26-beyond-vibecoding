using Microsoft.EntityFrameworkCore;

namespace UCK26.Api.Persistence;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Worksheet> Worksheets => Set<Worksheet>();
    public DbSet<WorkEntry> WorkEntries => Set<WorkEntry>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    internal List<(WorkEntry Entity, AuditLog Log)> PendingBackfills { get; } = [];

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(e =>
        {
            e.HasKey(u => u.Id);
            e.Property(u => u.UserName).IsRequired().HasMaxLength(64);
            e.Property(u => u.PasswordHash).IsRequired();
            e.Property(u => u.Role).IsRequired().HasMaxLength(32);
            e.HasIndex(u => u.UserName).IsUnique();
            e.HasData(SeedData.AdminUser);
        });

        modelBuilder.Entity<Worksheet>(e =>
        {
            e.HasKey(w => w.Id);
            e.Property(w => w.UserName).IsRequired().HasMaxLength(64);
            e.HasIndex(w => new { w.UserId, w.Year, w.Month }).IsUnique();
            e.HasMany(w => w.Entries)
                .WithOne(e => e.Worksheet)
                .HasForeignKey(e => e.WorksheetId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<WorkEntry>(e =>
        {
            e.HasKey(w => w.Id);
            e.Property(w => w.Type).IsRequired().HasMaxLength(32);
            e.Property(w => w.Description).IsRequired().HasMaxLength(256);
            e.HasIndex(w => new { w.WorksheetId, w.Date, w.Type });
        });
    }
}

public static class SeedData
{
    public const string AdminUserName = "admin";
    public const string AdminPassword = "Demo!2026";
    public const string AdminPasswordHash = "seed:Demo!2026";

    public static readonly User AdminUser = new()
    {
        Id = 1,
        UserName = AdminUserName,
        PasswordHash = AdminPasswordHash,
        Role = UserRoles.Admin,
        CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
    };
}
