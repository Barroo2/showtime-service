namespace ShowtimeService.Data;

public class Film
{
    public Guid Id { get; set; }
    public required string Title { get; set; }
    public int DurationMinutes { get; set; }
    public int AgeRating { get; set; }
    public required string Description { get; set; }
}

public class Hall
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public int RowCount { get; set; }
    public int SeatsPerRow { get; set; }
}

public class Showtime
{
    public Guid Id { get; set; }
    public Guid FilmId { get; set; }
    public Film Film { get; set; } = null!;
    public Guid HallId { get; set; }
    public Hall Hall { get; set; } = null!;
    public DateTime StartsAtUtc { get; set; }
    public decimal BasePrice { get; set; }
}
