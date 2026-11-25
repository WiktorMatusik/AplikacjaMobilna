using AplikacjaMobilna.Services;

namespace AplikacjaMobilna.Pages;

public partial class RegisterPage : ContentPage
{
    private readonly FirebaseAuthService _firebaseService;
    private readonly LocalDatabaseService _localDbService;

    public RegisterPage()
    {
        InitializeComponent();
        _firebaseService = new FirebaseAuthService();
        _localDbService = new LocalDatabaseService();
    }

    private async void OnRegisterClicked(object sender, EventArgs e)
    {
        string username = UsernameEntry.Text;
        string password = PasswordEntry.Text;
        string? selectedRole = RolePicker.SelectedItem?.ToString();

        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password) || string.IsNullOrEmpty(selectedRole))
        {
            await DisplayAlertAsync("Błąd", "Wypełnij wszystkie pola i wybierz rolę.", "OK");
            return;
        }

        if (password.Length < 6)
        {
            await DisplayAlertAsync("Błąd", "Hasło musi mieć minimum 6 znaków.", "OK");
            return;
        }

        var (success, message) = await _firebaseService.RegisterUserAsync(username, password, selectedRole);

        if (success)
        {
            await _localDbService.EnsureLocalUserProfileAsync(username.Trim(), selectedRole);

            await DisplayAlertAsync("Sukces", $"Konto dla {selectedRole} ({username}) zostało utworzone!", "OK");
            await Shell.Current.GoToAsync("..");
        }
        else
        {
            await DisplayAlertAsync("Błąd", message, "OK");
        }
    }

    private async void OnBackToLoginClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }
}