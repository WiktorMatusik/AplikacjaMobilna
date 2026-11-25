namespace AplikacjaMobilna.Services;

public static class WalkTrackingService
{
    private const string DistanceKey = "SavedWalkDistanceKm";
    private static readonly LocalDatabaseService _dbService = new();

    public static event Action<double>? DistanceChanged;
    public static event Action<TimeSpan>? TimeChanged;
    public static event Action<string>? StatusChanged;
    public static event Action<double, double, bool>? LocationUpdated;
    public static event Action? WalkReset;

    public static bool IsWalking { get; private set; }
    public static TimeSpan ElapsedTime { get; private set; } = TimeSpan.Zero;
    public static List<Location> RoutePoints { get; } = new();
    public static string CurrentStatus { get; private set; } = "GPS aktywny i gotowy";

    private static Location? _lastLocation;
    private static int _unsavedSeconds = 0;
    private static CancellationTokenSource? _timerCts;
    private static CancellationTokenSource? _gpsCts;

    public static double TotalDistanceKm
    {
        get => Preferences.Default.Get(DistanceKey, 0.0);
        set
        {
            Preferences.Default.Set(DistanceKey, value);
            DistanceChanged?.Invoke(value);
        }
    }

    public static string FormattedDistance => $"{TotalDistanceKm:F2} km";

    public static async Task StartWalkAsync()
    {
        if (IsWalking)
            return;

        var status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
        if (status != PermissionStatus.Granted)
        {
            status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
        }

        if (status != PermissionStatus.Granted)
        {
            UpdateStatus("Brak uprawnień do lokalizacji GPS");
            return;
        }

#if ANDROID
        var bgStatus = await Permissions.CheckStatusAsync<Permissions.LocationAlways>();
        if (bgStatus != PermissionStatus.Granted)
        {
            await Permissions.RequestAsync<Permissions.LocationAlways>();
        }
        Platforms.Android.WalkForegroundService.StartService();
#endif

        IsWalking = true;
        _lastLocation = RoutePoints.LastOrDefault();
        UpdateStatus("Spacer w toku (działa w tle)...");

        StartTimerLoop();
        StartGpsLoop();
    }

    public static async Task PauseWalkAsync()
    {
        if (!IsWalking)
            return;

        IsWalking = false;
        StopLoops();

#if ANDROID
        Platforms.Android.WalkForegroundService.StopService();
#endif

        await FlushUnsavedDurationAsync();
        UpdateStatus("Spacer wstrzymany");
    }

    public static async Task FinishAndResetWalkAsync()
    {
        IsWalking = false;
        StopLoops();

#if ANDROID
        Platforms.Android.WalkForegroundService.StopService();
#endif

        await FlushUnsavedDurationAsync();

        ElapsedTime = TimeSpan.Zero;
        _unsavedSeconds = 0;
        _lastLocation = null;
        RoutePoints.Clear();
        TotalDistanceKm = 0.0;

        TimeChanged?.Invoke(ElapsedTime);
        UpdateStatus("GPS aktywny i gotowy");
        WalkReset?.Invoke();
    }

    private static void StartTimerLoop()
    {
        _timerCts?.Cancel();
        _timerCts?.Dispose();
        _timerCts = new CancellationTokenSource();
        var token = _timerCts.Token;

        _ = Task.Run(async () =>
        {
            using var periodicTimer = new PeriodicTimer(TimeSpan.FromSeconds(1));
            try
            {
                while (await periodicTimer.WaitForNextTickAsync(token) && IsWalking)
                {
                    ElapsedTime = ElapsedTime.Add(TimeSpan.FromSeconds(1));
                    TimeChanged?.Invoke(ElapsedTime);

                    _unsavedSeconds++;
                    if (_unsavedSeconds >= 5)
                    {
                        _ = FlushUnsavedDurationAsync();
                    }
                }
            }
            catch (OperationCanceledException) { }
        }, token);
    }

    private static void StartGpsLoop()
    {
        _gpsCts?.Cancel();
        _gpsCts?.Dispose();
        _gpsCts = new CancellationTokenSource();
        var token = _gpsCts.Token;

        _ = Task.Run(async () =>
        {
            while (!token.IsCancellationRequested && IsWalking)
            {
                try
                {
                    await UpdateLocationOnceAsync();
                    await Task.Delay(3000, token);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch { }
            }
        }, token);
    }

    private static void StopLoops()
    {
        try
        {
            _timerCts?.Cancel();
            _timerCts?.Dispose();
            _timerCts = null;
        }
        catch { }

        try
        {
            _gpsCts?.Cancel();
            _gpsCts?.Dispose();
            _gpsCts = null;
        }
        catch { }
    }

    private static async Task UpdateLocationOnceAsync()
    {
        if (!IsWalking)
            return;

        try
        {
            Location? currentLocation = null;

            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                var request = new GeolocationRequest(GeolocationAccuracy.Best, TimeSpan.FromSeconds(5));
                currentLocation = await Geolocation.Default.GetLocationAsync(request);
            });

            if (!IsWalking || currentLocation == null)
                return;

            bool addedPoint = false;

            if (_lastLocation == null)
            {
                RoutePoints.Add(currentLocation);
                _lastLocation = currentLocation;
                addedPoint = true;
            }
            else
            {
                double segmentKm = Location.CalculateDistance(_lastLocation, currentLocation, DistanceUnits.Kilometers);
                if (!double.IsNaN(segmentKm) && segmentKm >= 0.002)
                {
                    TotalDistanceKm += segmentKm;
                    string currentUser = LocalDatabaseService.CurrentUsername ?? "admin";
                    await _dbService.AddWalkSegmentAsync(currentUser, segmentKm);

                    RoutePoints.Add(currentLocation);
                    _lastLocation = currentLocation;
                    addedPoint = true;
                }
            }

            LocationUpdated?.Invoke(currentLocation.Latitude, currentLocation.Longitude, addedPoint);
        }
        catch { }
    }

    public static async Task FlushUnsavedDurationAsync()
    {
        if (_unsavedSeconds > 0)
        {
            int toSave = _unsavedSeconds;
            _unsavedSeconds = 0;
            string currentUser = LocalDatabaseService.CurrentUsername ?? "admin";
            await _dbService.AddWalkDurationAsync(currentUser, toSave);
        }
    }

    private static void UpdateStatus(string status)
    {
        CurrentStatus = status;
        StatusChanged?.Invoke(status);
    }
}