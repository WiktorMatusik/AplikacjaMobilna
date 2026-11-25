using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AplikacjaMobilna.PageModels;

public partial class WalkPageViewModel : ObservableObject, IDisposable
{
    [ObservableProperty]
    private string _statusText = "GPS aktywny i gotowy";

    [ObservableProperty]
    private string _distanceText = "0.00 km";

    [ObservableProperty]
    private string _timeText = "00:00:00";

    private bool _isWalking = false;
    private bool _isNavigatingBack = false;
    private double _totalDistanceKm = 0.0;
    private TimeSpan _elapsedTime = TimeSpan.Zero;
    private Location? _lastLocation;

    private readonly IDispatcherTimer? _timer;
    private CancellationTokenSource? _gpsCts;

    public WalkPageViewModel()
    {
        _timer = Application.Current?.Dispatcher.CreateTimer();
        if (_timer != null)
        {
            _timer.Interval = TimeSpan.FromSeconds(1);
            _timer.Tick += OnTimerTick;
        }
    }

    [RelayCommand]
    private async Task ToggleWalk()
    {
        if (_isNavigatingBack)
            return;

        if (!_isWalking)
        {
            bool hasPermission = await EnsureLocationPermissionAsync();
            if (!hasPermission)
            {
                StatusText = "Brak uprawnień do lokalizacji GPS";
                return;
            }

            _isWalking = true;
            _lastLocation = null;
            StatusText = "Spacer w toku (rejestrowanie)...";

            await UpdateLocationAsync();
            _timer?.Start();
            StartBackgroundLocationLoop();
        }
        else
        {
            StopWalking("Spacer zatrzymany");
        }
    }

    private void StopWalking(string newStatus)
    {
        _isWalking = false;

        try
        {
            _timer?.Stop();
        }
        catch { }

        try
        {
            var cts = _gpsCts;
            _gpsCts = null;
            if (cts != null)
            {
                if (!cts.IsCancellationRequested)
                    cts.Cancel();
                cts.Dispose();
            }
        }
        catch { }

        StatusText = newStatus;
    }

    private void OnTimerTick(object? sender, EventArgs e)
    {
        if (!_isWalking || _isNavigatingBack)
            return;

        _elapsedTime = _elapsedTime.Add(TimeSpan.FromSeconds(1));
        TimeText = _elapsedTime.ToString(@"hh\:mm\:ss");
    }

    private void StartBackgroundLocationLoop()
    {
        try
        {
            _gpsCts?.Cancel();
            _gpsCts?.Dispose();
        }
        catch { }

        _gpsCts = new CancellationTokenSource();
        var token = _gpsCts.Token;

        _ = Task.Run(async () =>
        {
            while (!token.IsCancellationRequested && _isWalking && !_isNavigatingBack)
            {
                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(3), token);
                    if (token.IsCancellationRequested || !_isWalking || _isNavigatingBack)
                        break;

                    await MainThread.InvokeOnMainThreadAsync(async () =>
                    {
                        if (!token.IsCancellationRequested && _isWalking && !_isNavigatingBack)
                        {
                            await UpdateLocationAsync();
                        }
                    });
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch
                {
                    // Ignorujemy pojedynczy błąd odczytu GPS w trakcie trasy
                }
            }
        }, token);
    }

    private async Task UpdateLocationAsync()
    {
        if (!_isWalking || _isNavigatingBack)
            return;

        try
        {
            var request = new GeolocationRequest(GeolocationAccuracy.Best, TimeSpan.FromSeconds(5));
            Location? currentLocation = await Geolocation.Default.GetLocationAsync(request);

            if (!_isWalking || _isNavigatingBack)
                return;

            if (currentLocation == null)
            {
                StatusText = "Oczekiwanie na sygnał GPS...";
                return;
            }

            if (_lastLocation != null)
            {
                double segmentKm = Location.CalculateDistance(_lastLocation, currentLocation, DistanceUnits.Kilometers);

                // Filtr minimalnego ruchu (np. > 2 m), aby ograniczyć dryf GPS w miejscu
                if (!double.IsNaN(segmentKm) && segmentKm >= 0.002)
                {
                    _totalDistanceKm += segmentKm;
                    DistanceText = $"{_totalDistanceKm:F2} km";
                }
            }

            _lastLocation = currentLocation;
            if (_isWalking && !_isNavigatingBack)
            {
                StatusText = "Spacer w toku (rejestrowanie)...";
            }
        }
        catch (FeatureNotSupportedException)
        {
            StatusText = "GPS nie jest wspierany na tym urządzeniu";
        }
        catch (FeatureNotEnabledException)
        {
            StatusText = "Włącz lokalizację (GPS) w urządzeniu";
        }
        catch (PermissionException)
        {
            StatusText = "Brak uprawnień do lokalizacji GPS";
        }
        catch (Exception ex)
        {
            StatusText = $"Błąd GPS: {ex.Message}";
        }
    }

    private static async Task<bool> EnsureLocationPermissionAsync()
    {
        try
        {
            var status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
            if (status != PermissionStatus.Granted)
            {
                status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
            }
            return status == PermissionStatus.Granted;
        }
        catch
        {
            return false;
        }
    }

    [RelayCommand]
    private async Task GoBackAsync()
    {
        if (_isNavigatingBack)
            return;

        _isNavigatingBack = true;
        StopWalking("GPS aktywny i gotowy");

        try
        {
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                if (Shell.Current != null)
                {
                    await Shell.Current.GoToAsync("..");
                }
            });
        }
        catch
        {
            try
            {
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    if (Shell.Current != null)
                    {
                        await Shell.Current.GoToAsync("MainPage");
                    }
                });
            }
            catch
            {
                // Zapobiega wywaleniu aplikacji przy błędzie stosu nawigacji
            }
        }
        finally
        {
            _isNavigatingBack = false;
        }
    }

    public void Dispose()
    {
        _isNavigatingBack = true;
        StopWalking("GPS aktywny i gotowy");
        if (_timer != null)
        {
            _timer.Tick -= OnTimerTick;
        }
    }
}