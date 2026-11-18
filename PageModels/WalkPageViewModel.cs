using AplikacjaMobilna.Pages;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AplikacjaMobilna.PageModels;

public partial class WalkPageViewModel : ObservableObject
{
    [ObservableProperty]
    private string _statusText = "GPS aktywny i gotowy";

    [ObservableProperty]
    private string _distanceText = "0.00 km";

    [ObservableProperty]
    private string _timeText = "00:00:00";

    private bool _isWalking = false;

    [RelayCommand]
    private void ToggleWalk()
    {
        _isWalking = !_isWalking;
        StatusText = _isWalking ? "Spacer w toku (rejestrowanie)..." : "Spacer zatrzymany";
    }

    [RelayCommand]
    private async Task GoBackAsync()
    {
        if (Shell.Current.Navigation.NavigationStack.Count > 1)
        {
            await Shell.Current.GoToAsync("..");
        }
        else
        {
            await Shell.Current.GoToAsync($"///{nameof(MainPage)}");
        }
    }
}