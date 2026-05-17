using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using UCK26.Api.Persistence;

namespace UCK26.Api.Endpoints;

public record WorksheetDto(int Id, int UserId, string UserName, int Year, int Month, List<WorkEntryDto> Entries);
public record WorkEntryDto(int Id, DateOnly Date, string Type, decimal Hours);
public record UpsertWorkEntryRequest(DateOnly Date, string Type, decimal Hours);

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

        group.MapPut("/entries", async (
            UpsertWorkEntryRequest body,
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
                return Results.BadRequest(new { error = "Date, type and non-negative hours are required." });
            }

            var worksheet = await GetOrCreateWorksheet(db, userId, userName, body.Date.Year, body.Date.Month, ct);
            var entry = worksheet.Entries.FirstOrDefault(e => e.Date == body.Date && e.Type == body.Type);

            if (body.Hours == 0)
            {
                if (entry is not null)
                {
                    db.WorkEntries.Remove(entry);
                    await db.SaveChangesAsync(ct);
                }

                return Results.NoContent();
            }

            if (entry is null)
            {
                entry = new WorkEntry
                {
                    WorksheetId = worksheet.Id,
                    Date = body.Date,
                    Type = body.Type,
                    Hours = body.Hours
                };
                db.WorkEntries.Add(entry);
            }
            else
            {
                entry.Hours = body.Hours;
            }

            await db.SaveChangesAsync(ct);
            return Results.Ok(new WorkEntryDto(entry.Id, entry.Date, entry.Type, entry.Hours));
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
                .Select(e => new WorkEntryDto(e.Id, e.Date, e.Type, e.Hours))
                .ToList());

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

    private static bool IsValidEntry(UpsertWorkEntryRequest body) =>
        body.Type is WorkEntryTypes.Work or WorkEntryTypes.Holiday or WorkEntryTypes.Doctor
        && body.Hours >= 0
        && IsValidMonth(body.Date.Year, body.Date.Month);
}
