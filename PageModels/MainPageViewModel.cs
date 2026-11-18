using AplikacjaMobilna.Pages;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AplikacjaMobilna.PageModels;

public partial class MainPageViewModel : ObservableObject, IDisposable
{
    [ObservableProperty]
    private int _batteryLevel;

    [ObservableProperty]
    private string _currentTime = string.Empty;

    [ObservableProperty]
    private string _walkDistance = "3.2 km";

    [ObservableProperty]
    private bool _isCaregiverConnected = true;

    private readonly IDispatcherTimer? _timer;

    public MainPageViewModel()
    {
        UpdateBatteryLevel();
        UpdateCurrentTime();

        try
        {
            Battery.Default.BatteryInfoChanged += OnBatteryInfoChanged;
        }
        catch { }

        _timer = Application.Current?.Dispatcher.CreateTimer();
        if (_timer != null)
        {
            _timer.Interval = TimeSpan.FromSeconds(1);

            _timer.Tick += (s, e) =>
            {
                UpdateCurrentTime();
                if (DateTime.Now.Second % 10 == 0) UpdateBatteryLevel();
            };

            _timer.Start();
        }
    }

    private void OnBatteryInfoChanged(object? sender, BatteryInfoChangedEventArgs e)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            if (e.ChargeLevel >= 0)
            {
                BatteryLevel = (int)Math.Round(e.ChargeLevel * 100);
            }
        });
    }

    private void UpdateCurrentTime()
    {
        CurrentTime = DateTime.Now.ToString("HH:mm:ss");
    }

    private void UpdateBatteryLevel()
    {
        try
        {
            double level = Battery.Default.ChargeLevel;
            if (level >= 0)
            {
                BatteryLevel = (int)Math.Round(level * 100);
            }
            else
            {
                // Urządzenie bez baterii (np. PC stacjonarny) lub brak odczytu
                BatteryLevel = 100;
            }
        }
        catch
        {
            BatteryLevel = 100;
        }
    }

    [RelayCommand]
    private async Task TriggerSosAsync()
    {
        await Shell.Current.DisplayAlertAsync("SOS", "Wysyłanie sygnału ratunkowego do opiekuna...", "OK");
    }

    [RelayCommand]
    private async Task OpenCompassAsync()
    {
        await Shell.Current.GoToAsync(nameof(CompassPage));
    }

    [RelayCommand]
    private async Task OpenWalkAsync()
    {
        await Shell.Current.GoToAsync(nameof(WalkPage));
    }

    [RelayCommand]
    private async Task CallCaregiverAsync()
    {
        await Shell.Current.DisplayAlertAsync("Telefon", "Łączenie z opiekunem...", "OK");
    }

    [RelayCommand]
    private async Task LogoutAsync()
    {
        bool confirm = await Shell.Current.DisplayAlertAsync("Wylogowanie", "Czy na pewno chcesz się wylogować?", "Tak", "Nie");
        if (confirm)
        {
            await Shell.Current.GoToAsync($"/{nameof(LoginPage)}");
        }
    }

    public void Dispose()
    {
        _timer?.Stop();
        try
        {
            Battery.Default.BatteryInfoChanged -= OnBatteryInfoChanged;
        }
        catch { }
    }

    [RelayCommand]
    private async Task OpenMenuAsync()
    {
        await Shell.Current.GoToAsync(nameof(MenuPage));
    }
}