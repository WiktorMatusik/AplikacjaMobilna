using System.Reflection;
using System.Text.Json;
using Firebase.Auth;
using Firebase.Auth.Providers;
using Firebase.Database;
using Firebase.Database.Query;

namespace AplikacjaMobilna.Services;

public class FirebaseAuthService
{
    private readonly FirebaseAuthClient _authClient;
    private readonly FirebaseClient _dbClient;

    public FirebaseAuthService()
    {
        var (apiKey, authDomain, dbUrl) = LoadConfigFromGoogleServices();

        var config = new FirebaseAuthConfig
        {
            ApiKey = apiKey,
            AuthDomain = authDomain,
            Providers = new FirebaseAuthProvider[]
            {
                new EmailProvider()
            }
        };

        _authClient = new FirebaseAuthClient(config);

        // Przekazujemy token zalogowanego użytkownika do Realtime Database
        _dbClient = new FirebaseClient(dbUrl, new FirebaseOptions
        {
            AuthTokenAsyncFactory = async () =>
            {
                if (_authClient.User != null)
                {
                    return await _authClient.User.GetIdTokenAsync();
                }
                return string.Empty;
            }
        });
    }

    private static (string ApiKey, string AuthDomain, string DbUrl) LoadConfigFromGoogleServices()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = assembly.GetManifestResourceNames()
            .FirstOrDefault(n => n.EndsWith("google-services.json", StringComparison.OrdinalIgnoreCase))
            ?? throw new FileNotFoundException("Nie znaleziono pliku google-services.json jako EmbeddedResource.");

        using var stream = assembly.GetManifestResourceStream(resourceName)!;
        using var doc = JsonDocument.Parse(stream);
        var root = doc.RootElement;

        var projectInfo = root.GetProperty("project_info");
        string projectId = projectInfo.GetProperty("project_id").GetString() ?? "";
        string authDomain = $"{projectId}.firebaseapp.com";

        string dbUrl = projectInfo.TryGetProperty("firebase_url", out var urlProp)
            ? urlProp.GetString() ?? ""
            : $"https://{projectId}-default-rtdb.europe-west1.firebasedatabase.app/";

        string apiKey = root.GetProperty("client")[0]
            .GetProperty("api_key")[0]
            .GetProperty("current_key").GetString() ?? "";

        return (apiKey, authDomain, dbUrl);
    }

    private static string FormatToEmail(string username) =>
        username.Contains('@') ? username.Trim() : $"{username.Trim().ToLower()}@seniorapp.local";

    public async Task<(bool Success, string Message)> RegisterUserAsync(string username, string password, string role)
    {
        try
        {
            string email = FormatToEmail(username);
            var userCredential = await _authClient.CreateUserWithEmailAndPasswordAsync(email, password, username.Trim());
            string uid = userCredential.User.Uid;

            // Zapis profilu i roli użytkownika w Firebase Realtime Database
            await _dbClient
                .Child("Users")
                .Child(uid)
                .PutAsync(new UserProfileDto
                {
                    Username = username.Trim(),
                    Role = role
                });

            return (true, "Konto zostało utworzone w chmurze!");
        }
        catch (FirebaseAuthException ex)
        {
            return (false, $"Błąd rejestracji: {ex.Reason}");
        }
        catch (Exception ex)
        {
            return (false, $"Wystąpił błąd: {ex.Message}");
        }
    }

    public async Task<(bool Success, string? Role, string Message)> LoginUserAsync(string username, string password)
    {
        try
        {
            string email = FormatToEmail(username);
            var userCredential = await _authClient.SignInWithEmailAndPasswordAsync(email, password);
            string uid = userCredential.User.Uid;

            // Pobranie roli przypisanej do konta
            var profile = await _dbClient
                .Child("Users")
                .Child(uid)
                .OnceSingleAsync<UserProfileDto>();

            string role = profile?.Role ?? "Senior";

            // Zapisanie sesji lokalnie, aby reszta aplikacji (kompas, ustawienia) wiedziała kto jest zalogowany
            Preferences.Default.Set("FirebaseUid", uid);
            LocalDatabaseService.CurrentUsername = username.Trim();

            return (true, role, "Zalogowano pomyślnie");
        }
        catch (FirebaseAuthException)
        {
            return (false, null, "Nieprawidłowy login lub hasło.");
        }
        catch (Exception ex)
        {
            return (false, null, $"Błąd połączenia: {ex.Message}");
        }
    }

    public void Logout()
    {
        try
        {
            if (_authClient.User != null)
            {
                _authClient.SignOut();
            }
        }
        catch
        {
            // Ignorujemy błąd, jeśli instancja klienta nie miała wczytanej sesji w pamięci
        }
        finally
        {
            Preferences.Default.Remove("FirebaseUid");
            LocalDatabaseService.CurrentUsername = null;
        }
    }

    private class UserProfileDto
    {
        public string Username { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
    }
}