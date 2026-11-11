using SQLite;
using AplikacjaMobilna.Models;

namespace AplikacjaMobilna.Services;

public class LocalDatabaseService
{
    private SQLiteAsyncConnection? _database;

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
}