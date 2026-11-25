using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using AndroidX.Core.App;

namespace AplikacjaMobilna.Platforms.Android;

[Service(ForegroundServiceType = ForegroundService.TypeLocation, Exported = false)]
public class WalkForegroundService : Service
{
    private const string ChannelId = "walk_tracking_channel";
    private const int NotificationId = 1001;
    private PowerManager.WakeLock? _wakeLock;

    public override IBinder? OnBind(Intent? intent) => null;

    public override StartCommandResult OnStartCommand(Intent? intent, StartCommandFlags flags, int startId)
    {
        CreateNotificationChannel();

        var notification = new NotificationCompat.Builder(this, ChannelId)
            .SetContentTitle("Aplikacja Seniora – Spacer w toku")
            .SetContentText("Rejestrowanie trasy i czasu w tle...")
            .SetSmallIcon(global::Android.Resource.Drawable.IcMenuCompass)
            .SetOngoing(true)
            .Build();

        if (Build.VERSION.SdkInt >= BuildVersionCodes.Q)
        {
            StartForeground(NotificationId, notification, ForegroundService.TypeLocation);
        }
        else
        {
            StartForeground(NotificationId, notification);
        }

        // Utrzymanie pracy procesora dla GPS po zgaszeniu ekranu
        var powerManager = (PowerManager?)GetSystemService(PowerService);
        if (powerManager != null && (_wakeLock == null || !_wakeLock.IsHeld))
        {
            _wakeLock = powerManager.NewWakeLock(WakeLockFlags.Partial, "AplikacjaSeniora::WalkWakeLock");
            _wakeLock.Acquire();
        }

        return StartCommandResult.Sticky;
    }

    public override void OnDestroy()
    {
        if (_wakeLock != null && _wakeLock.IsHeld)
        {
            _wakeLock.Release();
        }
        base.OnDestroy();
    }

    private void CreateNotificationChannel()
    {
        if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
        {
            var channel = new NotificationChannel(
                ChannelId,
                "Monitorowanie spaceru",
                NotificationImportance.Low);

            var manager = (NotificationManager?)GetSystemService(NotificationService);
            manager?.CreateNotificationChannel(channel);
        }
    }

    public static void StartService()
    {
        var context = global::Android.App.Application.Context;
        var intent = new Intent(context, typeof(WalkForegroundService));

        if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
            context.StartForegroundService(intent);
        else
            context.StartService(intent);
    }

    public static void StopService()
    {
        var context = global::Android.App.Application.Context;
        var intent = new Intent(context, typeof(WalkForegroundService));
        context.StopService(intent);
    }
}