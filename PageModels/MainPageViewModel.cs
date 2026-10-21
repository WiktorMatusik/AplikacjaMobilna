using AplikacjaMobilna.Pages; 
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AplikacjaMobilna.PageModels;

public partial class MainPageViewModel : ObservableObject
{
    [ObservableProperty]
    private int _batteryLevel = 84;

    [ObservableProperty]
    private string _walkDistance = "3.2 km";

    [ObservableProperty]
    private bool _isCaregiverConnected = true;

    [RelayCommand]
    private async Task TriggerSosAsync()
    {
        await Shell.Current.DisplayAlert("SOS", "Wysyłanie sygnału ratunkowego do opiekuna...", "OK");
    }

    [RelayCommand]
    private async Task OpenCompassAsync()
    {
        // Przejście do strony kompasu
        await Shell.Current.GoToAsync(nameof(CompassPage));
    }

    [RelayCommand]
    private async Task OpenWalkAsync()
    {
        // Przejście do strony spaceru
        await Shell.Current.GoToAsync(nameof(WalkPage));
    }

    [RelayCommand]
    private async Task CallCaregiverAsync()
    {
        await Shell.Current.DisplayAlert("Telefon", "Łączenie z opiekunem...", "OK");
    }
}