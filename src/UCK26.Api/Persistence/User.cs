namespace UCK26.Api.Persistence;

public class User
{
    public int Id { get; set; }
    public required string UserName { get; set; }
    public required string PasswordHash { get; set; }
    public required string Role { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public static class UserRoles
{
    public const string Admin = "Admin";
    public const string User = "User";
}

public class Worksheet
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public required string UserName { get; set; }
    public int Year { get; set; }
    public int Month { get; set; }
    public List<WorkEntry> Entries { get; set; } = [];
}

public class WorkEntry
{
    public int Id { get; set; }
    public int WorksheetId { get; set; }
    public Worksheet Worksheet { get; set; } = null!;
    public DateOnly Date { get; set; }
    public required string Type { get; set; }
    public TimeOnly Start { get; set; }
    public TimeOnly End { get; set; }
    public string Description { get; set; } = "";
}

public static class WorkEntryTypes
{
    public const string Work = "work";
    public const string Holiday = "holiday";
    public const string Doctor = "doctor";
}
