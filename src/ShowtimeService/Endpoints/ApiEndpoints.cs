using System.Globalization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using ShowtimeService.Contracts;
using ShowtimeService.Data;

namespace ShowtimeService.Endpoints;

public static class ApiEndpoints
{
    public static void MapApiEndpoints(this IEndpointRouteBuilder app)
    {
        var v1 = app.MapGroup("/v1");

        v1.MapGet("/films", GetFilms)
            .WithName("GetFilms")
            .WithSummary("Alle Filme")
            .Produces<List<FilmDto>>(StatusCodes.Status200OK);

        v1.MapGet("/showtimes", GetShowtimes)
            .WithName("GetShowtimes")
            .WithSummary("Vorstellungen eines Tages (date=YYYY-MM-DD, UTC), ohne Parameter alle kommenden")
            .Produces<List<ShowtimeListItemDto>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest);

        v1.MapGet("/showtimes/{id:guid}", GetShowtimeById)
            .WithName("GetShowtimeById")
            .WithSummary("Eine Vorstellung mit Film, Saal, Preis und Sitzplan-Labels")
            .Produces<ShowtimeDetailDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);
    }

    private static async Task<Ok<List<FilmDto>>> GetFilms(ShowtimeDbContext db, CancellationToken ct)
    {
        var films = await db.Films
            .AsNoTracking()
            .OrderBy(f => f.Title)
            .Select(f => new FilmDto(f.Id, f.Title, f.DurationMinutes, f.AgeRating, f.Description))
            .ToListAsync(ct);

        return TypedResults.Ok(films);
    }

    private static async Task<Results<Ok<List<ShowtimeListItemDto>>, ProblemHttpResult>> GetShowtimes(
        string? date, ShowtimeDbContext db, CancellationToken ct)
    {
        var query = db.Showtimes.AsNoTracking();

        if (date is null)
        {
            var now = DateTime.UtcNow;
            query = query.Where(s => s.StartsAtUtc >= now);
        }
        else
        {
            if (!DateOnly.TryParseExact(date, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var day))
            {
                return TypedResults.Problem(
                    statusCode: StatusCodes.Status400BadRequest,
                    title: "Ungültiges Datum",
                    detail: $"Der Parameter 'date' muss das Format YYYY-MM-DD haben, erhalten: '{date}'.");
            }

            var from = day.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            var to = from.AddDays(1);
            query = query.Where(s => s.StartsAtUtc >= from && s.StartsAtUtc < to);
        }

        var showtimes = await query
            .OrderBy(s => s.StartsAtUtc)
            .Select(s => new ShowtimeListItemDto(s.Id, s.FilmId, s.Film.Title, s.HallId, s.Hall.Name, s.StartsAtUtc, s.BasePrice))
            .ToListAsync(ct);

        return TypedResults.Ok(showtimes);
    }

    private static async Task<Results<Ok<ShowtimeDetailDto>, ProblemHttpResult>> GetShowtimeById(
        Guid id, ShowtimeDbContext db, CancellationToken ct)
    {
        var s = await db.Showtimes
            .AsNoTracking()
            .Include(x => x.Film)
            .Include(x => x.Hall)
            .FirstOrDefaultAsync(x => x.Id == id, ct);

        if (s is null)
        {
            return TypedResults.Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Vorstellung nicht gefunden",
                detail: $"Es gibt keine Vorstellung mit der ID '{id}'.");
        }

        var dto = new ShowtimeDetailDto(
            s.Id,
            new ShowtimeFilmDto(s.Film.Id, s.Film.Title, s.Film.DurationMinutes, s.Film.AgeRating),
            new ShowtimeHallDto(s.Hall.Id, s.Hall.Name, s.Hall.RowCount, s.Hall.SeatsPerRow),
            s.StartsAtUtc,
            s.BasePrice,
            SeatLabels.For(s.Hall.RowCount, s.Hall.SeatsPerRow));

        return TypedResults.Ok(dto);
    }
}
