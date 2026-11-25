using System.Globalization;
using AplikacjaMobilna.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AplikacjaMobilna.PageModels;

public partial class WalkPageViewModel : ObservableObject, IDisposable
{
    private readonly LocalDatabaseService _dbService = new();
    private static readonly CultureInfo PolishCulture = new("pl-PL");

    // --- Zakładki ---
    [ObservableProperty] private bool _isMapTabVisible = true;
    [ObservableProperty] private bool _isStatsTabVisible = false;
    [ObservableProperty] private Color _mapTabBgColor = Color.FromArgb("#0F766E");
    [ObservableProperty] private Color _mapTabTextColor = Colors.White;
    [ObservableProperty] private Color _statsTabBgColor = Colors.Transparent;
    [ObservableProperty] private Color _statsTabTextColor = Color.FromArgb("#64748B");

    // --- Statystyki ---
    [ObservableProperty] private string _selectedPeriod = "Dzień";
    [ObservableProperty] private string _selectedPeriodTitle = "Podsumowanie dzisiaj";
    [ObservableProperty] private string _periodDistanceText = "0.00 km";
    [ObservableProperty] private string _periodDurationText = "0 min 0 sek";
    [ObservableProperty] private string _periodRangeDescription = $"Data: {DateTime.Now:dd.MM.yyyy}";

    [ObservableProperty] private string _todayDistanceText = "0.00 km";
    [ObservableProperty] private string _todayDurationText = "0 min 0 sek";
    [ObservableProperty] private string _weekDistanceText = "0.00 km";
    [ObservableProperty] private string _weekDurationText = "0 min 0 sek";
    [ObservableProperty] private string _monthDistanceText = "0.00 km";
    [ObservableProperty] private string _monthDurationText = "0 min 0 sek";
    [ObservableProperty] private string _yearDistanceText = "0.00 km";
    [ObservableProperty] private string _yearDurationText = "0 min 0 sek";

    [ObservableProperty] private Color _dayBtnBg = Color.FromArgb("#0F766E");
    [ObservableProperty] private Color _dayBtnText = Colors.White;
    [ObservableProperty] private Color _weekBtnBg = Color.FromArgb("#E2E8F0");
    [ObservableProperty] private Color _weekBtnText = Color.FromArgb("#1E293B");
    [ObservableProperty] private Color _monthBtnBg = Color.FromArgb("#E2E8F0");
    [ObservableProperty] private Color _monthBtnText = Color.FromArgb("#1E293B");
    [ObservableProperty] private Color _yearBtnBg = Color.FromArgb("#E2E8F0");
    [ObservableProperty] private Color _yearBtnText = Color.FromArgb("#1E293B");

    // --- Bieżący spacer ---
    [ObservableProperty] private string _statusText = WalkTrackingService.CurrentStatus;
    [ObservableProperty] private string _distanceText = WalkTrackingService.FormattedDistance;
    [ObservableProperty] private string _timeText = WalkTrackingService.ElapsedTime.ToString(@"hh\:mm\:ss");
    [ObservableProperty]
    private string _actionButtonText = WalkTrackingService.IsWalking
        ? "⏸ WSTRZYMAJ SPACER"
        : (WalkTrackingService.ElapsedTime > TimeSpan.Zero ? "▶ WZNÓW SPACER" : "▶ ROZPOCZNIJ SPACER");

    public event Action<double, double, bool>? LocationUpdated;
    public event Action? WalkReset;
    public List<Location> RoutePoints => WalkTrackingService.RoutePoints;

    public WalkPageViewModel()
    {
        WalkTrackingService.DistanceChanged += OnDistanceChanged;
        WalkTrackingService.TimeChanged += OnTimeChanged;
        WalkTrackingService.StatusChanged += OnStatusChanged;
        WalkTrackingService.LocationUpdated += OnServiceLocationUpdated;
        WalkTrackingService.WalkReset += OnServiceWalkReset;

        _ = RefreshStatisticsAsync();
    }

    private void OnDistanceChanged(double km) =>
        MainThread.BeginInvokeOnMainThread(() => DistanceText = $"{km:F2} km");

    private void OnTimeChanged(TimeSpan time) =>
        MainThread.BeginInvokeOnMainThread(() => TimeText = time.ToString(@"hh\:mm\:ss"));

    private void OnStatusChanged(string status) =>
        MainThread.BeginInvokeOnMainThread(() => StatusText = status);

    private void OnServiceLocationUpdated(double lat, double lon, bool add) =>
        LocationUpdated?.Invoke(lat, lon, add);

    private void OnServiceWalkReset() =>
        WalkReset?.Invoke();

    [RelayCommand]
    private async Task ToggleWalk()
    {
        if (!WalkTrackingService.IsWalking)
        {
            await WalkTrackingService.StartWalkAsync();
            if (WalkTrackingService.IsWalking)
                ActionButtonText = "⏸ WSTRZYMAJ SPACER";
        }
        else
        {
            await WalkTrackingService.PauseWalkAsync();
            ActionButtonText = "▶ WZNÓW SPACER";
        }
    }

    [RelayCommand]
    private async Task ResetWalk()
    {
        bool confirm = await Shell.Current.DisplayAlertAsync(
            "Zakończenie spaceru",
            "Czy zakończyć i wyzerować obecny spacer (dystans i czas pozostaną zapisane w statystykach)?",
            "Tak",
            "Nie");

        if (!confirm)
            return;

        await WalkTrackingService.FinishAndResetWalkAsync();
        ActionButtonText = "▶ ROZPOCZNIJ SPACER";
        await RefreshStatisticsAsync();
    }

    [RelayCommand]
    private async Task GoBackAsync()
    {
        if (Shell.Current != null)
            await Shell.Current.GoToAsync("..");
    }

    // --- Zakładki i statystyki ---

    [RelayCommand]
    private void ShowMapTab()
    {
        IsMapTabVisible = true;
        IsStatsTabVisible = false;
        MapTabBgColor = Color.FromArgb("#0F766E");
        MapTabTextColor = Colors.White;
        StatsTabBgColor = Colors.Transparent;
        StatsTabTextColor = ThemeService.IsDarkMode ? Color.FromArgb("#CCCCCC") : Color.FromArgb("#64748B");
    }

    [RelayCommand]
    private async Task ShowStatsTabAsync()
    {
        await WalkTrackingService.FlushUnsavedDurationAsync();
        IsMapTabVisible = false;
        IsStatsTabVisible = true;
        StatsTabBgColor = Color.FromArgb("#0F766E");
        StatsTabTextColor = Colors.White;
        MapTabBgColor = Colors.Transparent;
        MapTabTextColor = ThemeService.IsDarkMode ? Color.FromArgb("#CCCCCC") : Color.FromArgb("#64748B");
        await RefreshStatisticsAsync();
    }

    [RelayCommand]
    private async Task SelectPeriodAsync(string period)
    {
        SelectedPeriod = period;
        UpdatePeriodButtonsVisual();
        await RefreshStatisticsAsync();
    }

    private void UpdatePeriodButtonsVisual()
    {
        Color activeBg = Color.FromArgb("#0F766E");
        Color inactiveBg = ThemeService.IsDarkMode ? Color.FromArgb("#262626") : Color.FromArgb("#E2E8F0");
        Color inactiveText = ThemeService.IsDarkMode ? Colors.White : Color.FromArgb("#1E293B");

        DayBtnBg = SelectedPeriod == "Dzień" ? activeBg : inactiveBg;
        DayBtnText = SelectedPeriod == "Dzień" ? Colors.White : inactiveText;
        WeekBtnBg = SelectedPeriod == "Tydzień" ? activeBg : inactiveBg;
        WeekBtnText = SelectedPeriod == "Tydzień" ? Colors.White : inactiveText;
        MonthBtnBg = SelectedPeriod == "Miesiąc" ? activeBg : inactiveBg;
        MonthBtnText = SelectedPeriod == "Miesiąc" ? Colors.White : inactiveText;
        YearBtnBg = SelectedPeriod == "Rok" ? activeBg : inactiveBg;
        YearBtnText = SelectedPeriod == "Rok" ? Colors.White : inactiveText;
    }

    private static string FormatDurationPolish(TimeSpan duration)
    {
        int totalHours = (int)duration.TotalHours;
        return totalHours > 0
            ? $"{totalHours} godz. {duration.Minutes} min"
            : $"{duration.Minutes} min {duration.Seconds} sek";
    }

    public async Task RefreshStatisticsAsync()
    {
        string user = LocalDatabaseService.CurrentUsername ?? "admin";

        var dayStats = await _dbService.GetWalkStatsForPeriodAsync(user, "Dzień");
        var weekStats = await _dbService.GetWalkStatsForPeriodAsync(user, "Tydzień");
        var monthStats = await _dbService.GetWalkStatsForPeriodAsync(user, "Miesiąc");
        var yearStats = await _dbService.GetWalkStatsForPeriodAsync(user, "Rok");

        TodayDistanceText = $"{dayStats.DistanceKm:F2} km";
        TodayDurationText = FormatDurationPolish(dayStats.Duration);
        WeekDistanceText = $"{weekStats.DistanceKm:F2} km";
        WeekDurationText = FormatDurationPolish(weekStats.Duration);
        MonthDistanceText = $"{monthStats.DistanceKm:F2} km";
        MonthDurationText = FormatDurationPolish(monthStats.Duration);
        YearDistanceText = $"{yearStats.DistanceKm:F2} km";
        YearDurationText = FormatDurationPolish(yearStats.Duration);

        DateTime now = DateTime.Now;
        switch (SelectedPeriod)
        {
            case "Dzień":
                SelectedPeriodTitle = "Podsumowanie dzisiaj";
                PeriodDistanceText = TodayDistanceText;
                PeriodDurationText = TodayDurationText;
                PeriodRangeDescription = $"Dzień: {now:dd.MM.yyyy}";
                break;
            case "Tydzień":
                SelectedPeriodTitle = "Podsumowanie tygodnia";
                PeriodDistanceText = WeekDistanceText;
                PeriodDurationText = WeekDurationText;
                PeriodRangeDescription = "Od poniedziałku do dzisiaj";
                break;
            case "Miesiąc":
                SelectedPeriodTitle = "Podsumowanie miesiąca";
                PeriodDistanceText = MonthDistanceText;
                PeriodDurationText = MonthDurationText;
                string m = now.ToString("MMMM yyyy", PolishCulture);
                PeriodRangeDescription = $"Miesiąc: {char.ToUpper(m[0]) + m[1..]}";
                break;
            case "Rok":
                SelectedPeriodTitle = "Podsumowanie roku";
                PeriodDistanceText = YearDistanceText;
                PeriodDurationText = YearDurationText;
                PeriodRangeDescription = $"Rok: {now:yyyy}";
                break;
        }
    }

    public void Dispose()
    {
        WalkTrackingService.DistanceChanged -= OnDistanceChanged;
        WalkTrackingService.TimeChanged -= OnTimeChanged;
        WalkTrackingService.StatusChanged -= OnStatusChanged;
        WalkTrackingService.LocationUpdated -= OnServiceLocationUpdated;
        WalkTrackingService.WalkReset -= OnServiceWalkReset;
    }
}