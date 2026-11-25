using AplikacjaMobilna.Services;

namespace AplikacjaMobilna.Pages;

public partial class MenuPage : ContentPage
{
    private readonly FirebaseAuthService _firebaseService = new();
    private bool _isInitializing = true;

    public MenuPage()
    {
        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _isInitializing = true;

        DarkModeSwitch.IsToggled = ThemeService.IsDarkMode;
        string user = LocalDatabaseService.CurrentUsername ?? "admin";
        LoggedUserLabel.Text = $"Zalogowano jako: {user}";
        UpdateFontButtonsVisual();

        _isInitializing = false;
    }

    private void OnDarkModeToggled(object? sender, ToggledEventArgs e)
    {
        if (_isInitializing) return;
        ThemeService.IsDarkMode = e.Value;
        UpdateFontButtonsVisual();
    }

    private void OnFontNormalClicked(object? sender, EventArgs e)
    {
        ThemeService.FontScale = 1.0;
        UpdateFontButtonsVisual();
    }

    private void OnFontLargeClicked(object? sender, EventArgs e)
    {
        ThemeService.FontScale = 1.25;
        UpdateFontButtonsVisual();
    }

    private void OnFontExtraLargeClicked(object? sender, EventArgs e)
    {
        ThemeService.FontScale = 1.5;
        UpdateFontButtonsVisual();
    }

    private void UpdateFontButtonsVisual()
    {
        double current = ThemeService.FontScale;
        Color activeBg = Color.FromArgb("#0F766E");
        Color inactiveBg = ThemeService.IsDarkMode ? Color.FromArgb("#262626") : Color.FromArgb("#E2E8F0");
        Color inactiveText = ThemeService.IsDarkMode ? Colors.White : Color.FromArgb("#1E293B");

        FontNormalBtn.BackgroundColor = Math.Abs(current - 1.0) < 0.05 ? activeBg : inactiveBg;
        FontNormalBtn.TextColor = Math.Abs(current - 1.0) < 0.05 ? Colors.White : inactiveText;

        FontLargeBtn.BackgroundColor = Math.Abs(current - 1.25) < 0.05 ? activeBg : inactiveBg;
        FontLargeBtn.TextColor = Math.Abs(current - 1.25) < 0.05 ? Colors.White : inactiveText;

        FontExtraLargeBtn.BackgroundColor = Math.Abs(current - 1.5) < 0.05 ? activeBg : inactiveBg;
        FontExtraLargeBtn.TextColor = Math.Abs(current - 1.5) < 0.05 ? Colors.White : inactiveText;
    }

    private async void OnLogoutClicked(object? sender, EventArgs e)
    {
        try
        {
            bool confirm = await DisplayAlertAsync("Wylogowanie", "Czy na pewno chcesz się wylogować?", "Tak", "Nie");
            if (!confirm)
                return;

            _firebaseService.Logout();

            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                await Shell.Current.GoToAsync($"///{nameof(LoginPage)}");
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Błąd wylogowania: {ex}");
            Application.Current!.Windows[0].Page = new AppShell();
        }
    }
}