using AplikacjaMobilna.Pages;
using AplikacjaMobilna.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AplikacjaMobilna.PageModels;

public partial class CompassPageViewModel : ObservableObject
{
    [ObservableProperty]
    private double _arrowRotation;

    [ObservableProperty]
    private string _statusText = "Ładowanie danych domu...";

    [ObservableProperty]
    private string _distanceText = "Dystans do domu: -- m";

    private readonly LocalDatabaseService _dbService = new();
    private Location? _homeLocation;
    private Location? _currentLocation;
    private double _currentHeading = 0;
    private bool _isHomeMode = true;
    private CancellationTokenSource? _gpsCts;

    public CompassPageViewModel()
    {
        StartSensors();
    }

    public async Task LoadUserHomeAsync()
    {
        string currentUser = LocalDatabaseService.CurrentUsername ?? "admin";
        _homeLocation = await _dbService.GetUserHomeLocationAsync(currentUser);

        if (_homeLocation == null)
        {
            _isHomeMode = false;
            StatusText = "Brak zapisanego domu! Kliknij 'DODAJ DOM'";
            DistanceText = "Tryb: Północ magnetyczna";
        }
        else
        {
            _isHomeMode = true;
            StatusText = $"Tryb: Kierunek do DOMU ({currentUser}) 🏠";
            StartGpsTracking();
        }

        UpdateArrowRotation();
    }

    private void StartSensors()
    {
        try
        {
            if (Compass.Default.IsSupported && !Compass.Default.IsMonitoring)
            {
                Compass.Default.ReadingChanged += Compass_ReadingChanged;
                Compass.Default.Start(SensorSpeed.UI);
            }
            else if (!Compass.Default.IsSupported)
            {
                StatusText = "Brak czujnika kompasu w urządzeniu";
            }
        }
        catch (Exception ex)
        {
            StatusText = $"Błąd kompasu: {ex.Message}";
        }
    }

    private void StartGpsTracking()
    {
        if (_gpsCts != null && !_gpsCts.IsCancellationRequested)
            return;

        _gpsCts = new CancellationTokenSource();
        _ = TrackLocationLoopAsync(_gpsCts.Token);
    }

    private async Task TrackLocationLoopAsync(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            try
            {
                var request = new GeolocationRequest(GeolocationAccuracy.Best, TimeSpan.FromSeconds(4));
                var location = await Geolocation.Default.GetLocationAsync(request, token);

                if (location != null && _homeLocation != null)
                {
                    _currentLocation = location;
                    double meters = HomeLocationService.CalculateDistanceMeters(_currentLocation, _homeLocation);

                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        if (meters < 15)
                            DistanceText = "Jesteś w DOMU! 🎉";
                        else if (meters >= 1000)
                            DistanceText = $"Dystans do domu: {(meters / 1000.0):F2} km";
                        else
                            DistanceText = $"Dystans do domu: {meters:F0} m";
                    });

                    UpdateArrowRotation();
                }
            }
            catch { }

            await Task.Delay(3000, token);
        }
    }

    private void Compass_ReadingChanged(object? sender, CompassChangedEventArgs e)
    {
        _currentHeading = e.Reading.HeadingMagneticNorth;
        UpdateArrowRotation();
    }

    private void UpdateArrowRotation()
    {
        if (!_isHomeMode || _homeLocation == null || _currentLocation == null)
        {
            ArrowRotation = -_currentHeading;
            return;
        }

        double bearingToHome = HomeLocationService.CalculateBearing(_currentLocation, _homeLocation);
        ArrowRotation = bearingToHome - _currentHeading;
    }

    [RelayCommand]
    private void ToggleMode()
    {
        if (_homeLocation == null)
        {
            StatusText = "Najpierw kliknij 'DODAJ DOM' i wybierz punkt!";
            return;
        }

        _isHomeMode = !_isHomeMode;
        StatusText = _isHomeMode ? "Tryb: Kierunek do DOMU 🏠" : "Tryb: Północ magnetyczna 🧭";
        UpdateArrowRotation();
    }

    [RelayCommand]
    private async Task GoBackAsync()
    {
        StopSensors();

        if (Shell.Current.Navigation.NavigationStack.Count > 1)
        {
            await Shell.Current.GoToAsync("..");
        }
        else
        {
            await Shell.Current.GoToAsync($"///{nameof(MainPage)}");
        }
    }

    public void StopSensors()
    {
        try
        {
            _gpsCts?.Cancel();
            if (Compass.Default.IsMonitoring)
            {
                Compass.Default.ReadingChanged -= Compass_ReadingChanged;
                Compass.Default.Stop();
            }
        }
        catch { }
    }
}