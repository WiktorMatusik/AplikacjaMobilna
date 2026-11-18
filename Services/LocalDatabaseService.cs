using SQLite;
using AplikacjaMobilna.Models;

namespace AplikacjaMobilna.Services;

public class LocalDatabaseService
{
    private SQLiteAsyncConnection? _database;

    // Przechowuje nazwę aktualnie zalogowanego użytkownika w trakcie trwania sesji
    private const string SessionUserKey = "LoggedInUsername";

    // Trwale przechowuje nazwę zalogowanego użytkownika między uruchomieniami aplikacji
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

    public async Task<bool> RegisterUserAsync(string username, string password, string role)
    {
        await InitAsync();

        var existingUser = await _database!.Table<User>()
            .Where(u => u.Username == username)
            .FirstOrDefaultAsync();

        if (existingUser != null)
        {
            return false;
        }

        var newUser = new User
        {
            Username = username,
            Password = password,
            Role = role
        };

        int result = await _database.InsertAsync(newUser);
        return result > 0;
    }

    public async Task<string?> LoginUserAsync(string username, string password)
    {
        await InitAsync();

        var user = await _database!.Table<User>()
            .Where(u => u.Username == username && u.Password == password)
            .FirstOrDefaultAsync();

        return user?.Role;
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
        else if (username == "admin")
        {
            await _database.InsertAsync(new User
            {
                Username = "admin",
                Password = "admin",
                Role = "Opiekun",
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