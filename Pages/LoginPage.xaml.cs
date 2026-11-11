namespace AplikacjaMobilna.Pages;

public partial class LoginPage : ContentPage
{
    public LoginPage()
    {
        InitializeComponent();
    }

    private async void OnLoginClicked(object sender, EventArgs e)
    {
        string username = UsernameEntry.Text;
        string password = PasswordEntry.Text;

        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            await DisplayAlert("Błąd", "Wprowadź login i hasło.", "OK");
            return;
        }

        if (username == "admin" && password == "admin")
        {
            await Shell.Current.GoToAsync($"/{nameof(MainPage)}");
        }
        else
        {
            await DisplayAlert("Błąd", "Nieprawidłowy login lub hasło.", "OK");
        }
    }

    private async void OnGoToRegisterClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(RegisterPage));
    }
}