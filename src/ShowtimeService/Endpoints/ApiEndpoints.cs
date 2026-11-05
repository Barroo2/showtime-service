using System.Globalization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using ShowtimeService.Contracts;
using ShowtimeService.Data;

namespace ShowtimeService.Endpoints;

public static class ApiEndpoints
{
    private const string Currency = "CHF";

    // Deprecation-Header (RFC 9745): seit wann v1 als veraltet gilt
    private static readonly string V1DeprecatedSince =
        $"@{new DateTimeOffset(2026, 11, 3, 0, 0, 0, TimeSpan.Zero).ToUnixTimeSeconds()}";

    public static void MapApiEndpoints(this IEndpointRouteBuilder app)
    {
        // v1 bleibt unverändert, bis kein Konsument sie mehr aufruft (Expand and Contract)
        var v1 = app.MapGroup("/v1")
            .WithGroupName("v1")
            .AddEndpointFilter(MarkV1Deprecated);

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

        var v2 = app.MapGroup("/v2").WithGroupName("v2");

        v2.MapGet("/films", GetFilms)
            .WithName("GetFilmsV2")
            .WithSummary("Alle Filme")
            .Produces<List<FilmDto>>(StatusCodes.Status200OK);

        v2.MapGet("/showtimes", GetShowtimesV2)
            .WithName("GetShowtimesV2")
            .WithSummary("Vorstellungen eines Tages (date=YYYY-MM-DD, UTC), ohne Parameter alle kommenden")
            .Produces<List<ShowtimeListItemV2Dto>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest);

        v2.MapGet("/showtimes/{id:guid}", GetShowtimeByIdV2)
            .WithName("GetShowtimeByIdV2")
            .WithSummary("Eine Vorstellung mit Film, Saal, Preis (Betrag und Währung) und Sitzplan-Labels")
            .Produces<ShowtimeDetailV2Dto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);
    }

    // Das Log zeigt, wer v1 noch nutzt und ab wann sie entfernt werden kann
    private static async ValueTask<object?> MarkV1Deprecated(
        EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var http = context.HttpContext;
        http.Response.Headers["Deprecation"] = V1DeprecatedSince;
        http.RequestServices.GetRequiredService<ILoggerFactory>()
            .CreateLogger("ShowtimeService.Deprecation")
            .LogWarning("Veraltete API aufgerufen: {Path}", http.Request.Path);

        return await next(context);
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

        if (FilterByDate(ref query, date) is { } problem)
        {
            return problem;
        }

        var showtimes = await query
            .OrderBy(s => s.StartsAtUtc)
            .Select(s => new ShowtimeListItemDto(s.Id, s.FilmId, s.Film.Title, s.HallId, s.Hall.Name, s.StartsAtUtc, s.BasePrice))
            .ToListAsync(ct);

        return TypedResults.Ok(showtimes);
    }

    private static async Task<Results<Ok<List<ShowtimeListItemV2Dto>>, ProblemHttpResult>> GetShowtimesV2(
        string? date, ShowtimeDbContext db, CancellationToken ct)
    {
        var query = db.Showtimes.AsNoTracking();

        if (FilterByDate(ref query, date) is { } problem)
        {
            return problem;
        }

        var showtimes = await query
            .OrderBy(s => s.StartsAtUtc)
            .Select(s => new ShowtimeListItemV2Dto(s.Id, s.FilmId, s.Film.Title, s.HallId, s.Hall.Name, s.StartsAtUtc, new PriceDto(s.BasePrice, Currency)))
            .ToListAsync(ct);

        return TypedResults.Ok(showtimes);
    }

    private static async Task<Results<Ok<ShowtimeDetailDto>, ProblemHttpResult>> GetShowtimeById(
        Guid id, ShowtimeDbContext db, CancellationToken ct)
    {
        var s = await FindShowtime(id, db, ct);

        if (s is null)
        {
            return ShowtimeNotFound(id);
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

    private static async Task<Results<Ok<ShowtimeDetailV2Dto>, ProblemHttpResult>> GetShowtimeByIdV2(
        Guid id, ShowtimeDbContext db, CancellationToken ct)
    {
        var s = await FindShowtime(id, db, ct);

        if (s is null)
        {
            return ShowtimeNotFound(id);
        }

        var dto = new ShowtimeDetailV2Dto(
            s.Id,
            new ShowtimeFilmDto(s.Film.Id, s.Film.Title, s.Film.DurationMinutes, s.Film.AgeRating),
            new ShowtimeHallDto(s.Hall.Id, s.Hall.Name, s.Hall.RowCount, s.Hall.SeatsPerRow),
            s.StartsAtUtc,
            new PriceDto(s.BasePrice, Currency),
            SeatLabels.For(s.Hall.RowCount, s.Hall.SeatsPerRow));

        return TypedResults.Ok(dto);
    }

    private static ProblemHttpResult? FilterByDate(ref IQueryable<Showtime> query, string? date)
    {
        if (date is null)
        {
            var now = DateTime.UtcNow;
            query = query.Where(s => s.StartsAtUtc >= now);
            return null;
        }

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
        return null;
    }

    private static Task<Showtime?> FindShowtime(Guid id, ShowtimeDbContext db, CancellationToken ct) =>
        db.Showtimes
            .AsNoTracking()
            .Include(x => x.Film)
            .Include(x => x.Hall)
            .FirstOrDefaultAsync(x => x.Id == id, ct);

    private static ProblemHttpResult ShowtimeNotFound(Guid id) =>
        TypedResults.Problem(
            statusCode: StatusCodes.Status404NotFound,
            title: "Vorstellung nicht gefunden",
            detail: $"Es gibt keine Vorstellung mit der ID '{id}'.");
}
