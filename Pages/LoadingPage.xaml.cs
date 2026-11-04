namespace AplikacjaMobilna.Pages;

public partial class LoadingPage : ContentPage
{
    public LoadingPage()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // Symulacja ładowania (np. pobieranie danych z sieci, sprawdzanie sesji) - 2 sekundy
        await Task.Delay(2000);

        // Po załadowaniu przechodzimy do ekranu logowania
        await Shell.Current.GoToAsync($"/{nameof(LoginPage)}");
    }
}