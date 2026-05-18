using System.Security.Claims;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace UCK26.Api.Persistence;

public sealed class AuditInterceptor(IHttpContextAccessor httpContextAccessor) : SaveChangesInterceptor
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is AppDbContext db)
        {
            AddAuditLogs(db);
        }

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void AddAuditLogs(AppDbContext db)
    {
        var entries = db.ChangeTracker.Entries<WorkEntry>()
            .Where(e => e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            .ToList();

        foreach (var entry in entries)
        {
            db.AuditLogs.Add(new AuditLog
            {
                EntityId = GetValue<int>(entry, nameof(WorkEntry.Id)),
                Action = ToAction(entry.State),
                PerformedBy = GetPerformedBy(),
                PerformedAt = DateTime.UtcNow,
                ChangedFields = BuildChangedFields(entry),
                EntryDate = GetValue<DateOnly>(entry, nameof(WorkEntry.Date)).ToString("yyyy-MM-dd"),
                WorksheetId = GetValue<int>(entry, nameof(WorkEntry.WorksheetId))
            });
        }
    }

    private string GetPerformedBy()
    {
        var user = httpContextAccessor.HttpContext?.User;
        return user?.FindFirstValue(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Name)
            ?? user?.FindFirstValue(ClaimTypes.Name)
            ?? "system";
    }

    private static AuditAction ToAction(EntityState state) =>
        state switch
        {
            EntityState.Added => AuditAction.Create,
            EntityState.Modified => AuditAction.Update,
            EntityState.Deleted => AuditAction.Delete,
            _ => throw new InvalidOperationException($"Cannot audit state {state}.")
        };

    private static string? BuildChangedFields(EntityEntry<WorkEntry> entry)
    {
        if (entry.State == EntityState.Deleted)
        {
            return null;
        }

        var fields = entry.Properties
            .Where(p => p.Metadata.Name != nameof(WorkEntry.Id))
            .Where(p => entry.State == EntityState.Added || p.IsModified)
            .Select(p => new AuditChangedField(
                p.Metadata.Name,
                entry.State == EntityState.Added ? null : FormatValue(p.OriginalValue),
                FormatValue(p.CurrentValue)))
            .ToList();

        return JsonSerializer.Serialize(fields, JsonOptions);
    }

    private static T GetValue<T>(EntityEntry<WorkEntry> entry, string propertyName)
    {
        var property = entry.Property(propertyName);
        var value = entry.State == EntityState.Deleted ? property.OriginalValue : property.CurrentValue;
        return (T)value!;
    }

    private static string? FormatValue(object? value) =>
        value switch
        {
            null => null,
            DateOnly date => date.ToString("yyyy-MM-dd"),
            TimeOnly time => time.ToString("HH:mm:ss"),
            _ => value.ToString()
        };
}
