using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System.Text.Json;

namespace UCK26.Api.Persistence;

public class AuditInterceptor(IHttpContextAccessor httpContextAccessor) : SaveChangesInterceptor
{
    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is AppDbContext db)
            AttachAuditLogs(db);
        return await base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void AttachAuditLogs(AppDbContext db)
    {
        var user = httpContextAccessor.HttpContext?.User;
        var performer = user?.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Name)?.Value
            ?? user?.Identity?.Name
            ?? "system";
        var now = DateTime.UtcNow;

        foreach (var entry in db.ChangeTracker.Entries<WorkEntry>().ToList())
        {
            if (entry.State is not (EntityState.Added or EntityState.Modified or EntityState.Deleted))
                continue;

            var action = entry.State switch
            {
                EntityState.Added => AuditAction.Create,
                EntityState.Modified => AuditAction.Update,
                _ => AuditAction.Delete
            };

            List<AuditChangedField>? fields = null;

            if (action == AuditAction.Create)
            {
                fields = entry.Properties
                    .Where(p => p.Metadata.Name is not "Id")
                    .Select(p => new AuditChangedField(p.Metadata.Name, null, p.CurrentValue?.ToString()))
                    .ToList();
            }
            else if (action == AuditAction.Update)
            {
                fields = entry.Properties
                    .Where(p => p.IsModified)
                    .Select(p => new AuditChangedField(
                        p.Metadata.Name,
                        p.OriginalValue?.ToString(),
                        p.CurrentValue?.ToString()))
                    .ToList();
            }

            var rawDate = entry.Property(nameof(WorkEntry.Date)).CurrentValue
                       ?? entry.Property(nameof(WorkEntry.Date)).OriginalValue;
            var entryDate = rawDate is DateOnly d ? d.ToString("yyyy-MM-dd") : rawDate?.ToString();
            var entryType = entry.Property(nameof(WorkEntry.Type)).CurrentValue?.ToString()
                         ?? entry.Property(nameof(WorkEntry.Type)).OriginalValue?.ToString();
            var worksheetId = (int?)entry.Property(nameof(WorkEntry.WorksheetId)).CurrentValue
                           ?? (int?)entry.Property(nameof(WorkEntry.WorksheetId)).OriginalValue;

            db.AuditLogs.Add(new AuditLog
            {
                EntityType = "WorkEntry",
                EntityId = entry.Entity.Id,
                Action = action,
                PerformedBy = performer,
                PerformedAt = now,
                ChangedFields = fields is { Count: > 0 }
                    ? JsonSerializer.Serialize(fields)
                    : null,
                EntryDate = entryDate,
                EntryType = entryType,
                WorksheetId = worksheetId,
            });
        }
    }
}
