# ADR: Work Entry Audit Log

## Status
Proposed

## Context

WorkEntry mutations (create, update, delete) need tracking so worksheet owners + admins can inspect full change history per day. History must survive deletions — deleted entry still appears in timeline.

## Decisions

### 1. Capture — EF Core SaveChanges interceptor

**Decision:** `ISaveChangesInterceptor` (`AuditInterceptor`) registered on `AppDbContext`.

**Rationale:** Captures all mutations in one place regardless of which endpoint triggered them. `ChangeTracker` provides original + current values without extra queries. No missed audit if endpoint logic refactored.

**Rejected:** Application-layer audit calls inside each endpoint — fragile, duplicated, easy to miss.

### 2. AuditLog entity shape

```csharp
public class AuditLog
{
    public int Id { get; set; }
    public string EntityType { get; set; }      // "WorkEntry"
    public int EntityId { get; set; }           // WorkEntry.Id at time of action
    public AuditAction Action { get; set; }     // Create | Update | Delete
    public string PerformedBy { get; set; }     // username — stored at write time
    public DateTime PerformedAt { get; set; }   // UTC
    public string? ChangedFields { get; set; }  // JSON: [{field, oldValue, newValue}]
    // Denormalized snapshot for deleted entries
    public string? EntryDate { get; set; }      // WorkEntry.Date
    public int? WorksheetId { get; set; }       // WorkEntry.WorksheetId
}

public enum AuditAction { Create, Update, Delete }
```

**Rationale for denormalization:** Deleted WorkEntry loses FK. Storing `EntryDate` + `WorksheetId` lets API query history for a day after deletion.

`ChangedFields` JSON: only changed fields on update, all initial values on create, empty on delete.

### 3. PerformedBy — stored username string

**Decision:** Store `name` claim from `HttpContext.User` at write time.

**Rationale:** Human-readable without join. Survives user account deletion. Matches existing JWT claim from `JwtTokenService`. No HTTP context (seeding) → `"system"`.

**How:** `IHttpContextAccessor` injected into `AuditInterceptor` via DI.

### 4. API endpoint

```
GET /api/worksheets/{year}/{month}/days/{date}/audit
```

Optional `?userId={id}` — same auth pattern as `WorksheetEndpoints`. Any authenticated user, ownership enforced inside handler. Returns descending by `PerformedAt`.

### 5. No UI filtering

All events for day, all entry types, descending chronological. No filter controls for MVP.

### 6. Migration

Single migration `AddAuditLog`. No changes to existing entities. No FK to `WorkEntry` — intentional.

## Consequences

- Every `SaveChanges` incurs interceptor overhead; negligible at demo scale.
- `ChangedFields` stored as `TEXT` in SQLite — no schema enforcement on field names; acceptable.
- Wipe `uck26.db` resets all audit history.
