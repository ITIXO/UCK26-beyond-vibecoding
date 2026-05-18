# Developer Handover: Work Entry Audit Log

## Overview

Track WorkEntry mutations (create/update/delete) via EF Core interceptor. Per-day history sidebar in worksheet UI visible to worksheet owner + admin.

---

## Backend

### 1. Entity — `src/UCK26.Api/Persistence/AuditLog.cs`

```csharp
namespace UCK26.Api.Persistence;

public enum AuditAction { Create, Update, Delete }

public class AuditLog
{
    public int Id { get; set; }
    public string EntityType { get; set; } = "WorkEntry";
    public int EntityId { get; set; }
    public AuditAction Action { get; set; }
    public string PerformedBy { get; set; } = "";
    public DateTime PerformedAt { get; set; }
    public string? ChangedFields { get; set; }   // JSON
    public string? EntryDate { get; set; }        // denormalized WorkEntry.Date
    public int? WorksheetId { get; set; }         // denormalized WorkEntry.WorksheetId
}

public record AuditChangedField(string Field, string? OldValue, string? NewValue);
```

### 2. DbContext — `src/UCK26.Api/Persistence/AppDbContext.cs`

```csharp
public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
```

### 3. Migration

```bash
cd src/UCK26.Api
dotnet ef migrations add AddAuditLog
```

No FK from `AuditLog` to `WorkEntry` — intentional.

### 4. Interceptor — `src/UCK26.Api/Persistence/AuditInterceptor.cs`

```csharp
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
            await AttachAuditLogsAsync(db);
        return await base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private Task AttachAuditLogsAsync(AppDbContext db)
    {
        var performer = httpContextAccessor.HttpContext?.User.FindFirst("name")?.Value ?? "system";
        var now = DateTime.UtcNow;

        foreach (var entry in db.ChangeTracker.Entries<WorkEntry>())
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

            var entryDate = entry.Property(nameof(WorkEntry.Date)).CurrentValue?.ToString()
                         ?? entry.Property(nameof(WorkEntry.Date)).OriginalValue?.ToString();
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
                WorksheetId = worksheetId,
            });
        }

        return Task.CompletedTask;
    }
}
```

Register in `Program.cs`:

```csharp
builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton<AuditInterceptor>();
builder.Services.AddDbContext<AppDbContext>((sp, options) =>
{
    options.UseSqlite(connectionString);
    options.AddInterceptors(sp.GetRequiredService<AuditInterceptor>());
});
```

### 5. Endpoint — `src/UCK26.Api/Endpoints/WorksheetEndpoints.cs`

Add inside `MapWorksheets()`:

```csharp
group.MapGet("/{year:int}/{month:int}/days/{date}/audit", async (
    int year, int month, string date,
    [FromQuery] int? userId,
    ClaimsPrincipal principal,
    AppDbContext db,
    CancellationToken ct) =>
{
    var callerId = int.Parse(principal.FindFirstValue(ClaimTypes.NameIdentifier)!);
    var isAdmin = principal.IsInRole("Admin");
    var targetUserId = isAdmin && userId.HasValue ? userId.Value : callerId;

    var worksheet = await db.Worksheets
        .FirstOrDefaultAsync(w => w.UserId == targetUserId && w.Year == year && w.Month == month, ct);

    if (worksheet is null)
        return Results.Ok(Array.Empty<object>());

    var logs = await db.AuditLogs
        .Where(a => a.WorksheetId == worksheet.Id && a.EntryDate == date)
        .OrderByDescending(a => a.PerformedAt)
        .Select(a => new
        {
            a.EntityId,
            a.Action,
            a.PerformedBy,
            a.PerformedAt,
            ChangedFields = a.ChangedFields != null
                ? JsonSerializer.Deserialize<List<AuditChangedField>>(a.ChangedFields)
                : null,
        })
        .ToListAsync(ct);

    return Results.Ok(logs);
})
.RequireAuthorization();
```

---

## Frontend

### 1. Contracts — `src/uck26-frontend/src/shared/lib/api/worksheets.contracts.api.ts`

```ts
export type AuditAction = 'Create' | 'Update' | 'Delete';

export interface AuditChangedFieldDto {
  field: string;
  oldValue: string | null;
  newValue: string | null;
}

export interface AuditLogEntryDto {
  entityId: number;
  action: AuditAction;
  performedBy: string;
  performedAt: string; // ISO
  changedFields: AuditChangedFieldDto[] | null;
}
```

### 2. API call — `src/uck26-frontend/src/shared/lib/api/worksheets.api.ts`

```ts
export function getWorkEntryAudit(
  year: number,
  month: number,
  date: string,
  userId?: number
): Promise<AuditLogEntryDto[]> {
  const params = userId ? `?userId=${userId}` : '';
  return apiFetch(`/api/worksheets/${year}/${month}/days/${date}/audit${params}`);
}
```

### 3. Query hook — `src/uck26-frontend/src/features/work/worksheets.queries.ts`

```ts
export function useWorkEntryAudit(
  year: number,
  month: number,
  date: string | null,
  userId?: number
) {
  return useQuery({
    queryKey: ['worksheet-audit', year, month, date, userId],
    queryFn: () => getWorkEntryAudit(year, month, date!, userId),
    enabled: date !== null,
  });
}
```

### 4. Sidebar — `src/uck26-frontend/src/features/work/WorkEntryAuditSidebar.tsx`

Props:
```ts
interface WorkEntryAuditSidebarProps {
  open: boolean;
  date: string | null;      // "2026-05-06"
  dateLabel: string;        // "Tue 06/05"
  userId?: number;
  year: number;
  month: number;
  onClose: () => void;
}
```

Renders overlay `Drawer` from itixo component library. Header: history icon + date label + close button. Body: timeline from `useWorkEntryAudit`. Per event: colored dot (green=Create, blue=Update, red=Delete) + entry type badge + performer + timestamp. Collapsible — latest expanded, one open at a time. Empty state: "No history for this day." Loading state: skeleton rows.

### 5. WorkSheet wiring — `src/uck26-frontend/src/features/work/WorkSheet.tsx`

```tsx
const [auditDate, setAuditDate] = useState<string | null>(null);

// In each TableRow for a day — add final TableCell:
<IconButton
  variant="ghost"
  size="mini"
  aria-label={`Show history for ${day.label}`}
  data-test-id={`work-history-${day.date}`}
  onClick={() => setAuditDate(prev => prev === day.date ? null : day.date)}
>
  <History className={auditDate === day.date ? 'text-blue-500' : undefined} />
</IconButton>

// Add History import from lucide-react
// Add extra <TableHead /> for button column
// Add <WorkEntryAuditSidebar> at bottom of return
```

---

## Tests

### Backend — `src/UCK26.Api.Tests/Integration/WorksheetEndpointTests.cs`

- `GET audit returns 200 empty array when no entries`
- `GET audit returns create event after entry added`
- `GET audit returns update event with field diffs`
- `GET audit records delete event`
- `GET audit for other user forbidden for non-admin`
- `GET audit for other user allowed for admin with userId param`

### UI — `src/UCK26.Ui.Tests/WorkPageTests.cs`

- Click history button → sidebar appears (`data-test-id="work-history-sidebar"`)
- Sidebar shows event after entry created
- Click different day → sidebar switches date
- Click same button again → sidebar closes

---

## data-test-id additions

| Element | ID |
|---|---|
| History button per row | `work-history-{date}` |
| Audit sidebar | `work-history-sidebar` |
| Timeline event | `work-audit-event-{index}` |

---

## Checklist

- [ ] `AuditLog` entity + `AuditAction` enum
- [ ] `AppDbContext.AuditLogs` DbSet
- [ ] Migration `AddAuditLog`
- [ ] `AuditInterceptor` + DI registration
- [ ] `GET /{year}/{month}/days/{date}/audit` endpoint
- [ ] TS contracts (`AuditLogEntryDto`, `AuditChangedFieldDto`)
- [ ] `getWorkEntryAudit` API fn
- [ ] `useWorkEntryAudit` hook
- [ ] `WorkEntryAuditSidebar` component
- [ ] `WorkSheet` history button + sidebar wiring
- [ ] Backend integration tests
- [ ] Playwright UI tests
- [ ] Docs updated (README, AGENTS.md, CLAUDE.md)
