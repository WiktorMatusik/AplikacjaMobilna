using AplikacjaMobilna.Services;

namespace AplikacjaMobilna.Pages;

public partial class RegisterPage : ContentPage
{
    private readonly LocalDatabaseService _dbService;

    public RegisterPage()
    {
        InitializeComponent();
        _dbService = new LocalDatabaseService();
    }

    private async void OnRegisterClicked(object sender, EventArgs e)
    {
        string username = UsernameEntry.Text;
        string password = PasswordEntry.Text;
        string selectedRole = RolePicker.SelectedItem?.ToString();

        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password) || string.IsNullOrEmpty(selectedRole))
        {
            await DisplayAlert("Błąd", "Wypełnij wszystkie pola i wybierz rolę.", "OK");
            return;
        }

        bool success = await _dbService.RegisterUserAsync(username, password, selectedRole);

        if (success)
        {
            await DisplayAlert("Sukces", $"Konto dla {selectedRole} ({username}) zostało utworzone lokalnie!", "OK");
            await Shell.Current.GoToAsync(nameof(LoginPage));
        }
        else
        {
            await DisplayAlert("Błąd", "Użytkownik o takiej nazwie już istnieje w pamięci.", "OK");
        }
    }

    private async void OnBackToLoginClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(LoginPage));
    }
}