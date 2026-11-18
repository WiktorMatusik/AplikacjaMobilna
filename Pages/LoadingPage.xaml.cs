using AplikacjaMobilna.Services;

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

        await Task.Delay(1500);

        if (!string.IsNullOrEmpty(LocalDatabaseService.CurrentUsername))
        {
            await Shell.Current.GoToAsync($"///{nameof(MainPage)}");
        }
        else
        {
            await Shell.Current.GoToAsync($"///{nameof(LoginPage)}");
        }
    }
}