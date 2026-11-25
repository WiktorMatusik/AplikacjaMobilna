using AplikacjaMobilna.Services;

namespace AplikacjaMobilna.Pages;

public partial class LoginPage : ContentPage
{
    private readonly FirebaseAuthService _firebaseService;
    private readonly LocalDatabaseService _localDbService;

    public LoginPage()
    {
        InitializeComponent();
        _firebaseService = new FirebaseAuthService();
        _localDbService = new LocalDatabaseService();
    }

    private async void OnLoginClicked(object sender, EventArgs e)
    {
        string username = UsernameEntry.Text;
        string password = PasswordEntry.Text;

        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            await DisplayAlertAsync("Błąd", "Wprowadź login i hasło.", "OK");
            return;
        }

        // Konto awaryjne offline
        if (username == "admin" && password == "admin")
        {
            LocalDatabaseService.CurrentUsername = "admin";
            await Shell.Current.GoToAsync($"///{nameof(MainPage)}");
            return;
        }

        var (success, role, message) = await _firebaseService.LoginUserAsync(username, password);

        if (success)
        {
            await _localDbService.EnsureLocalUserProfileAsync(username.Trim(), role ?? "Senior");

            await DisplayAlertAsync("Sukces", $"Zalogowano pomyślnie! Rola: {role}", "OK");
            await Shell.Current.GoToAsync($"///{nameof(MainPage)}");
        }
        else
        {
            await DisplayAlertAsync("Błąd", message, "OK");
        }
    }

    private async void OnGoToRegisterClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(RegisterPage));
    }
}