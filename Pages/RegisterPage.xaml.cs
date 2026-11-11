namespace AplikacjaMobilna.Pages;

public partial class RegisterPage : ContentPage
{
    public RegisterPage()
    {
        InitializeComponent();
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

        await DisplayAlert("Sukces", $"Konto dla {selectedRole} ({username}) zostało utworzone!", "OK");

        await Shell.Current.GoToAsync(nameof(LoginPage));
    }

    private async void OnBackToLoginClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(LoginPage));
    }
}