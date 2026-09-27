namespace ShowtimeService.Data;

public static class SeedData
{
    // Feste IDs: bleiben nach Neustart gleich, der Booking Service arbeitet damit.
    public static readonly Guid FilmInterstellarId = Guid.Parse("11111111-1111-1111-1111-000000000001");
    public static readonly Guid FilmInsideOut2Id = Guid.Parse("11111111-1111-1111-1111-000000000002");

    public static readonly Guid Hall1Id = Guid.Parse("22222222-2222-2222-2222-000000000001");
    public static readonly Guid Hall2Id = Guid.Parse("22222222-2222-2222-2222-000000000002");

    public static readonly Guid Showtime1Id = Guid.Parse("33333333-3333-3333-3333-000000000001");
    public static readonly Guid Showtime2Id = Guid.Parse("33333333-3333-3333-3333-000000000002");
    public static readonly Guid Showtime3Id = Guid.Parse("33333333-3333-3333-3333-000000000003");
    public static readonly Guid Showtime4Id = Guid.Parse("33333333-3333-3333-3333-000000000004");

    public static void Seed(ShowtimeDbContext db)
    {
        if (db.Films.Any())
            return;

        db.Films.AddRange(
            new Film
            {
                Id = FilmInterstellarId,
                Title = "Interstellar",
                DurationMinutes = 169,
                AgeRating = 12,
                Description = "Eine Gruppe von Astronauten reist durch ein Wurmloch, um eine neue Heimat für die Menschheit zu finden."
            },
            new Film
            {
                Id = FilmInsideOut2Id,
                Title = "Alles steht Kopf 2",
                DurationMinutes = 96,
                AgeRating = 0,
                Description = "Riley wird Teenager und in ihrem Kopf ziehen neue Emotionen ein."
            });

        db.Halls.AddRange(
            new Hall { Id = Hall1Id, Name = "Saal 1", RowCount = 5, SeatsPerRow = 10 },
            new Hall { Id = Hall2Id, Name = "Saal 2", RowCount = 8, SeatsPerRow = 12 });

        var times = ScheduleFrom(DateTime.UtcNow.Date);

        db.Showtimes.AddRange(
            new Showtime { Id = Showtime1Id, FilmId = FilmInterstellarId, HallId = Hall1Id, StartsAtUtc = times[Showtime1Id], BasePrice = 18.50m },
            new Showtime { Id = Showtime2Id, FilmId = FilmInsideOut2Id, HallId = Hall2Id, StartsAtUtc = times[Showtime2Id], BasePrice = 15.00m },
            new Showtime { Id = Showtime3Id, FilmId = FilmInsideOut2Id, HallId = Hall1Id, StartsAtUtc = times[Showtime3Id], BasePrice = 16.00m },
            new Showtime { Id = Showtime4Id, FilmId = FilmInterstellarId, HallId = Hall2Id, StartsAtUtc = times[Showtime4Id], BasePrice = 22.00m });

        db.SaveChanges();
    }

    // Liegt die spaeteste Vorstellung in der Vergangenheit, werden die 4 Seed-Vorstellungen
    // auf dieselben Uhrzeiten von heute und morgen verschoben. Nur StartsAtUtc wird geaendert.
    public static void RefreshShowtimeDates(ShowtimeDbContext db)
    {
        var now = DateTime.UtcNow;
        var latest = db.Showtimes.Max(s => (DateTime?)s.StartsAtUtc);
        if (latest is null || latest >= now)
            return;

        var times = ScheduleFrom(now.Date);
        var ids = times.Keys.ToList();
        foreach (var showtime in db.Showtimes.Where(s => ids.Contains(s.Id)))
            showtime.StartsAtUtc = times[showtime.Id];

        db.SaveChanges();
    }

    // Seed-Uhrzeiten (UTC): zwei Vorstellungen heute, zwei morgen
    private static Dictionary<Guid, DateTime> ScheduleFrom(DateTime todayUtc)
    {
        var tomorrow = todayUtc.AddDays(1);
        return new Dictionary<Guid, DateTime>
        {
            [Showtime1Id] = todayUtc.AddHours(17),
            [Showtime2Id] = todayUtc.AddHours(20).AddMinutes(30),
            [Showtime3Id] = tomorrow.AddHours(14),
            [Showtime4Id] = tomorrow.AddHours(19),
        };
    }
}
