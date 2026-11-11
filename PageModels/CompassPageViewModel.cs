using AplikacjaMobilna.Pages;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AplikacjaMobilna.PageModels;

public partial class CompassPageViewModel : ObservableObject
{
    [ObservableProperty]
    private double _arrowRotation;

    [ObservableProperty]
    private string _statusText = "Uruchamianie kompasu...";

    [ObservableProperty]
    private string _distanceText = "Kompas aktywny";

    public CompassPageViewModel()
    {
        ToggleCompass(true);
    }

    private void ToggleCompass(bool enable)
    {
        try
        {
            if (Compass.Default.IsSupported)
            {
                if (!Compass.Default.IsMonitoring)
                {
                    Compass.Default.ReadingChanged += Compass_ReadingChanged;
                    Compass.Default.Start(SensorSpeed.UI);
                    StatusText = "Kompas działa poprawnie";
                }
            }
            else
            {
                StatusText = "Kompas nie jest wspierany na tym urządzeniu";
            }
        }
        catch (Exception ex)
        {
            StatusText = $"Błąd czujnika: {ex.Message}";
        }
    }

    private void Compass_ReadingChanged(object sender, CompassChangedEventArgs e)
    {
        double heading = e.Reading.HeadingMagneticNorth;
        ArrowRotation = -heading;
    }

    [RelayCommand]
    private void ToggleMode()
    {
        StatusText = StatusText.Contains("Północ") ? "Tryb: Szukanie domu" : "Tryb: Północ magnetyczna";
    }

    [RelayCommand]
    private async Task GoBackAsync()
    {
        try
        {
            if (Compass.Default.IsMonitoring)
            {
                Compass.Default.ReadingChanged -= Compass_ReadingChanged;
                Compass.Default.Stop();
            }
        }
        catch { }

        await Shell.Current.GoToAsync($"/{nameof(MainPage)}");
    }
}