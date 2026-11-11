using FirebirdSql.Data.FirebirdClient;

namespace AplikacjaMobilna.Services;

public class DatabaseService
{
    private readonly string _connectionString = "User=SYSDBA;Password=TwojeHasloSerwera;Database=/sciezka/na/serwerze/AplikacjaSeniora.fdb;Server=adres-ip-lub-domena-chmury.pl;Port=3050;ServerType=0;Charset=UTF8;";

    public bool RegisterUser(string username, string password, string role)
    {
        try
        {
            using var connection = new FbConnection(_connectionString);
            connection.Open();

            string query = "INSERT INTO Users (Username, PasswordHash, Role) VALUES (@username, @password, @role)";
            using var command = new FbCommand(query, connection);

            command.Parameters.AddWithValue("@username", username);
            command.Parameters.AddWithValue("@password", password);
            command.Parameters.AddWithValue("@role", role);

            command.ExecuteNonQuery();
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Błąd rejestracji w chmurze: {ex.Message}");
            return false;
        }
    }

    public string? LoginUser(string username, string password)
    {
        try
        {
            using var connection = new FbConnection(_connectionString);
            connection.Open();

            string query = "SELECT Role FROM Users WHERE Username = @username AND PasswordHash = @password";
            using var command = new FbCommand(query, connection);

            command.Parameters.AddWithValue("@username", username);
            command.Parameters.AddWithValue("@password", password);

            using var reader = command.ExecuteReader();
            if (reader.Read())
            {
                return reader["Role"].ToString();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Błąd logowania z chmurą: {ex.Message}");
        }
        return null;
    }
}