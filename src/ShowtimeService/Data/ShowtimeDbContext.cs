using System.Text;
using Microsoft.EntityFrameworkCore;

namespace ShowtimeService.Data;

public class ShowtimeDbContext(DbContextOptions<ShowtimeDbContext> options) : DbContext(options)
{
    public DbSet<Film> Films => Set<Film>();
    public DbSet<Hall> Halls => Set<Hall>();
    public DbSet<Showtime> Showtimes => Set<Showtime>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Film>(e =>
        {
            e.ToTable("films");
            e.Property(f => f.Title).HasMaxLength(200);
            e.Property(f => f.Description).HasMaxLength(2000);
        });

        modelBuilder.Entity<Hall>(e =>
        {
            e.ToTable("halls");
            e.Property(h => h.Name).HasMaxLength(100);
        });

        modelBuilder.Entity<Showtime>(e =>
        {
            e.ToTable("showtimes");
            e.Property(s => s.StartsAtUtc).HasColumnType("timestamptz");
            e.Property(s => s.BasePrice).HasPrecision(6, 2);
            e.HasOne(s => s.Film).WithMany().HasForeignKey(s => s.FilmId);
            e.HasOne(s => s.Hall).WithMany().HasForeignKey(s => s.HallId);
            e.HasIndex(s => s.StartsAtUtc);
        });

        ApplySnakeCase(modelBuilder);
    }

    // Spalten, Keys, FKs und Indizes in snake_case (ohne Zusatzpaket)
    private static void ApplySnakeCase(ModelBuilder modelBuilder)
    {
        foreach (var entity in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var property in entity.GetProperties())
                property.SetColumnName(ToSnakeCase(property.Name));

            foreach (var key in entity.GetKeys())
                key.SetName(ToSnakeCase(key.GetName()!));

            foreach (var fk in entity.GetForeignKeys())
                fk.SetConstraintName(ToSnakeCase(fk.GetConstraintName()!));

            foreach (var index in entity.GetIndexes())
                index.SetDatabaseName(ToSnakeCase(index.GetDatabaseName()!));
        }
    }

    private static string ToSnakeCase(string name)
    {
        var sb = new StringBuilder(name.Length + 8);
        for (var i = 0; i < name.Length; i++)
        {
            var c = name[i];
            if (char.IsUpper(c))
            {
                if (i > 0 && name[i - 1] != '_' && !char.IsUpper(name[i - 1]))
                    sb.Append('_');
                sb.Append(char.ToLowerInvariant(c));
            }
            else
            {
                sb.Append(c);
            }
        }
        return sb.ToString();
    }
}
