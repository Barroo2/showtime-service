namespace ShowtimeService.Contracts;

public record FilmDto(Guid Id, string Title, int DurationMinutes, int AgeRating, string Description);

public record ShowtimeListItemDto(
    Guid Id,
    Guid FilmId,
    string FilmTitle,
    Guid HallId,
    string HallName,
    DateTime StartsAtUtc,
    decimal Price);

public record ShowtimeFilmDto(Guid Id, string Title, int DurationMinutes, int AgeRating);

public record ShowtimeHallDto(Guid Id, string Name, int RowCount, int SeatsPerRow);

public record ShowtimeDetailDto(
    Guid Id,
    ShowtimeFilmDto Film,
    ShowtimeHallDto Hall,
    DateTime StartsAtUtc,
    decimal Price,
    IReadOnlyList<string> SeatLabels);
