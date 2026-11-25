using SQLite;
using AplikacjaMobilna.Models;

namespace AplikacjaMobilna.Services;

public class LocalDatabaseService
{
    private SQLiteAsyncConnection? _database;
    private const string SessionUserKey = "LoggedInUsername";

    public static string? CurrentUsername
    {
        get
        {
            string saved = Preferences.Default.Get(SessionUserKey, string.Empty);
            return string.IsNullOrEmpty(saved) ? null : saved;
        }
        set
        {
            if (string.IsNullOrEmpty(value))
                Preferences.Default.Remove(SessionUserKey);
            else
                Preferences.Default.Set(SessionUserKey, value);
        }
    }

    private async Task InitAsync()
    {
        if (_database != null)
            return;

        var dbPath = Path.Combine(FileSystem.AppDataDirectory, "AplikacjaSeniora.db3");
        _database = new SQLiteAsyncConnection(dbPath);

        await _database.CreateTableAsync<User>();
        await _database.CreateTableAsync<WalkEntry>();
    }

    public async Task EnsureLocalUserProfileAsync(string username, string role)
    {
        await InitAsync();

        var existingUser = await _database!.Table<User>()
            .Where(u => u.Username == username)
            .FirstOrDefaultAsync();

        if (existingUser == null)
        {
            await _database.InsertAsync(new User
            {
                Username = username,
                Role = role
            });
        }
        else if (existingUser.Role != role)
        {
            existingUser.Role = role;
            await _database.UpdateAsync(existingUser);
        }
    }

    public async Task SaveUserHomeLocationAsync(string username, double latitude, double longitude)
    {
        await InitAsync();

        var user = await _database!.Table<User>()
            .Where(u => u.Username == username)
            .FirstOrDefaultAsync();

        if (user != null)
        {
            user.HomeLatitude = latitude;
            user.HomeLongitude = longitude;
            await _database.UpdateAsync(user);
        }
        else
        {
            await _database.InsertAsync(new User
            {
                Username = username,
                Role = username == "admin" ? "Opiekun" : "Senior",
                HomeLatitude = latitude,
                HomeLongitude = longitude
            });
        }
    }

    public async Task<Location?> GetUserHomeLocationAsync(string username)
    {
        await InitAsync();

        var user = await _database!.Table<User>()
            .Where(u => u.Username == username)
            .FirstOrDefaultAsync();

        if (user?.HomeLatitude != null && user?.HomeLongitude != null)
        {
            return new Location(user.HomeLatitude.Value, user.HomeLongitude.Value);
        }

        return null;
    }

    // --- METODY DO STATYSTYK SPACERÓW (DYSTANS I CZAS) ---

    public async Task AddWalkSegmentAsync(string username, double distanceKm)
    {
        if (distanceKm <= 0 || double.IsNaN(distanceKm))
            return;

        await InitAsync();

        await _database!.InsertAsync(new WalkEntry
        {
            Username = username,
            Date = DateTime.Now,
            DistanceKm = distanceKm,
            DurationSeconds = 0
        });
    }

    public async Task AddWalkDurationAsync(string username, int seconds)
    {
        if (seconds <= 0)
            return;

        await InitAsync();

        await _database!.InsertAsync(new WalkEntry
        {
            Username = username,
            Date = DateTime.Now,
            DistanceKm = 0,
            DurationSeconds = seconds
        });
    }

    public async Task<(double DistanceKm, TimeSpan Duration)> GetWalkStatsForPeriodAsync(string username, string period)
    {
        await InitAsync();

        DateTime now = DateTime.Now;
        DateTime startDate = period switch
        {
            "Dzień" => now.Date,
            "Tydzień" => StartOfWeek(now, DayOfWeek.Monday),
            "Miesiąc" => new DateTime(now.Year, now.Month, 1),
            "Rok" => new DateTime(now.Year, 1, 1),
            _ => now.Date
        };

        var entries = await _database!.Table<WalkEntry>()
            .Where(w => w.Username == username && w.Date >= startDate)
            .ToListAsync();

        double totalKm = entries.Sum(e => e.DistanceKm);
        int totalSeconds = entries.Sum(e => e.DurationSeconds);

        return (totalKm, TimeSpan.FromSeconds(totalSeconds));
    }

    public async Task ClearUserWalksAsync(string username)
    {
        await InitAsync();
        await _database!.ExecuteAsync("DELETE FROM WalkEntry WHERE Username = ?", username);
    }

    private static DateTime StartOfWeek(DateTime dt, DayOfWeek startOfWeek)
    {
        int diff = (7 + (dt.DayOfWeek - startOfWeek)) % 7;
        return dt.AddDays(-1 * diff).Date;
    }
}