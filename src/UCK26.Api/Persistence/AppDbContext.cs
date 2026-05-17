using Microsoft.EntityFrameworkCore;

namespace UCK26.Api.Persistence;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Worksheet> Worksheets => Set<Worksheet>();
    public DbSet<WorkEntry> WorkEntries => Set<WorkEntry>();

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
            e.Property(w => w.Hours).HasPrecision(5, 2);
            e.HasIndex(w => new { w.WorksheetId, w.Date, w.Type }).IsUnique();
        });
    }
}
