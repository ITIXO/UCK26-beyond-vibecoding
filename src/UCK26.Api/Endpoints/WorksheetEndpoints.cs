using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using UCK26.Api.Persistence;

namespace UCK26.Api.Endpoints;

public record WorksheetDto(int Id, int UserId, string UserName, int Year, int Month, List<WorkEntryDto> Entries);
public record WorkEntryDto(int Id, DateOnly Date, string Type, TimeOnly Start, TimeOnly End, string Description, decimal Hours);
public record CreateWorkEntryRequest(DateOnly Date, string Type, TimeOnly Start, TimeOnly End, string? Description);
public record UpdateWorkEntryRequest(TimeOnly Start, TimeOnly End, string? Description);

public static class WorksheetEndpoints
{
    public static IEndpointRouteBuilder MapWorksheets(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/worksheets")
            .WithTags("Worksheets")
            .RequireAuthorization();

        group.MapGet("/", async (
            int year,
            int month,
            ClaimsPrincipal principal,
            AppDbContext db,
            CancellationToken ct) =>
        {
            if (!TryGetUser(principal, out var userId, out var userName))
            {
                return Results.Unauthorized();
            }

            if (!IsValidMonth(year, month))
            {
                return Results.BadRequest(new { error = "Year and month are required." });
            }

            var worksheet = await GetOrCreateWorksheet(db, userId, userName, year, month, ct);
            return Results.Ok(ToDto(worksheet));
        });

        group.MapPost("/entries", async (
            CreateWorkEntryRequest body,
            ClaimsPrincipal principal,
            AppDbContext db,
            CancellationToken ct) =>
        {
            if (!TryGetUser(principal, out var userId, out var userName))
            {
                return Results.Unauthorized();
            }

            if (!IsValidEntry(body))
            {
                return Results.BadRequest(new { error = "Date, type, start and end are required." });
            }

            var worksheet = await GetOrCreateWorksheet(db, userId, userName, body.Date.Year, body.Date.Month, ct);
            var entry = new WorkEntry
            {
                WorksheetId = worksheet.Id,
                Date = body.Date,
                Type = body.Type,
                Start = body.Start,
                End = body.End,
                Description = body.Description?.Trim() ?? ""
            };
            db.WorkEntries.Add(entry);

            await db.SaveChangesAsync(ct);
            return Results.Created($"/api/worksheets/entries/{entry.Id}", ToDto(entry));
        });

        group.MapPut("/entries/{id:int}", async (
            int id,
            UpdateWorkEntryRequest body,
            ClaimsPrincipal principal,
            AppDbContext db,
            CancellationToken ct) =>
        {
            if (!TryGetUser(principal, out var userId, out _))
            {
                return Results.Unauthorized();
            }

            if (!IsValidTimeRange(body.Start, body.End))
            {
                return Results.BadRequest(new { error = "End must be after start." });
            }

            var entry = await db.WorkEntries
                .Include(e => e.Worksheet)
                .FirstOrDefaultAsync(e => e.Id == id && e.Worksheet.UserId == userId, ct);
            if (entry is null)
            {
                return Results.NotFound();
            }

            entry.Start = body.Start;
            entry.End = body.End;
            entry.Description = body.Description?.Trim() ?? "";

            await db.SaveChangesAsync(ct);
            return Results.Ok(ToDto(entry));
        });

        group.MapDelete("/entries/{id:int}", async (
            int id,
            ClaimsPrincipal principal,
            AppDbContext db,
            CancellationToken ct) =>
        {
            if (!TryGetUser(principal, out var userId, out _))
            {
                return Results.Unauthorized();
            }

            var entry = await db.WorkEntries
                .Include(e => e.Worksheet)
                .FirstOrDefaultAsync(e => e.Id == id && e.Worksheet.UserId == userId, ct);
            if (entry is null)
            {
                return Results.NotFound();
            }

            db.WorkEntries.Remove(entry);
            await db.SaveChangesAsync(ct);
            return Results.NoContent();
        });

        return app;
    }

    private static async Task<Worksheet> GetOrCreateWorksheet(
        AppDbContext db,
        int userId,
        string userName,
        int year,
        int month,
        CancellationToken ct)
    {
        var worksheet = await db.Worksheets
            .Include(w => w.Entries)
            .FirstOrDefaultAsync(w => w.UserId == userId && w.Year == year && w.Month == month, ct);

        if (worksheet is not null)
        {
            return worksheet;
        }

        worksheet = new Worksheet
        {
            UserId = userId,
            UserName = userName,
            Year = year,
            Month = month
        };
        db.Worksheets.Add(worksheet);
        await db.SaveChangesAsync(ct);
        return worksheet;
    }

    private static WorksheetDto ToDto(Worksheet worksheet) =>
        new(
            worksheet.Id,
            worksheet.UserId,
            worksheet.UserName,
            worksheet.Year,
            worksheet.Month,
            worksheet.Entries
                .OrderBy(e => e.Date)
                .ThenBy(e => e.Type)
                .ThenBy(e => e.Start)
                .Select(ToDto)
                .ToList());

    private static WorkEntryDto ToDto(WorkEntry entry) =>
        new(
            entry.Id,
            entry.Date,
            entry.Type,
            entry.Start,
            entry.End,
            entry.Description,
            (decimal)(entry.End - entry.Start).TotalHours);

    private static bool TryGetUser(ClaimsPrincipal principal, out int userId, out string userName)
    {
        var idClaim = principal.FindFirstValue(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)
            ?? principal.FindFirstValue(ClaimTypes.NameIdentifier);
        userName = principal.FindFirstValue(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Name)
            ?? principal.Identity?.Name
            ?? "";

        return int.TryParse(idClaim, out userId) && !string.IsNullOrWhiteSpace(userName);
    }

    private static bool IsValidMonth(int year, int month) =>
        year is >= 2000 and <= 2100 && month is >= 1 and <= 12;

    private static bool IsValidEntry(CreateWorkEntryRequest body) =>
        body.Type is WorkEntryTypes.Work or WorkEntryTypes.Holiday or WorkEntryTypes.Doctor
        && IsValidTimeRange(body.Start, body.End)
        && IsValidMonth(body.Date.Year, body.Date.Month);

    private static bool IsValidTimeRange(TimeOnly start, TimeOnly end) => end > start;
}
