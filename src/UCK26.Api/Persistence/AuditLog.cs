namespace UCK26.Api.Persistence;

public enum AuditAction
{
    Create,
    Update,
    Delete
}

public class AuditLog
{
    public int Id { get; set; }
    public string EntityType { get; set; } = "WorkEntry";
    public int EntityId { get; set; }
    public AuditAction Action { get; set; }
    public string PerformedBy { get; set; } = "";
    public DateTime PerformedAt { get; set; }
    public string? ChangedFields { get; set; }
    public string? EntryDate { get; set; }
    public int? WorksheetId { get; set; }
}

public record AuditChangedField(string Field, string? OldValue, string? NewValue);
