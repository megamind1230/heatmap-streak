using HeatMapStreak.Web.Data;
using Microsoft.EntityFrameworkCore;

namespace HeatMapStreak.Tests;

/// <summary>
/// Each instance is an isolated SQLite database file in the temp folder, migrated with
/// the real migrations so the tests exercise the same schema the app runs on.
/// </summary>
public sealed class TestDatabase : IDisposable
{
    /// <summary>Id of the account every test habit belongs to.</summary>
    public const string DefaultUserId = "user-1";

    private readonly string _path = Path.Combine(Path.GetTempPath(), $"heatmapstreak-test-{Guid.NewGuid():N}.db");

    public string ConnectionString => $"Data Source={_path}";

    public TestDatabase()
    {
        using var db = CreateContext();
        db.Database.Migrate();

        // Habit.UserId is a foreign key, so a real account must exist first.
        EnsureUser(DefaultUserId);
    }

    public ApplicationDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(ConnectionString)
            .Options);

    /// <summary>Inserts an account with the given Id when it does not exist yet.</summary>
    public void EnsureUser(string id)
    {
        using var db = CreateContext();
        if (db.Users.Any(u => u.Id == id))
            return;

        db.Users.Add(new ApplicationUser
        {
            Id = id,
            UserName = id,
            NormalizedUserName = id.ToUpperInvariant(),
            Email = $"{id}@example.com",
            NormalizedEmail = $"{id}@example.com".ToUpperInvariant(),
            EmailConfirmed = true
        });
        db.SaveChanges();
    }

    public void Dispose()
    {
        foreach (var suffix in new[] { "", "-wal", "-shm" })
        {
            try
            {
                File.Delete(_path + suffix);
            }
            catch
            {
                // A leftover temp file is not worth failing a test run over.
            }
        }
    }
}
