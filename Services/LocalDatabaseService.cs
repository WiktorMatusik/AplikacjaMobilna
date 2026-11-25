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
}