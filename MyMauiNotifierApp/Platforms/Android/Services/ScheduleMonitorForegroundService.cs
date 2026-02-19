using Android.App;
using Android.Content;
using Android.OS;
using AndroidX.Core.App;
using System.Text.Json;
using MyMauiNotifierApp.Models;
using MyMauiNotifierApp.Services;
using MyMauiNotifierApp;

namespace MyMauiNotifierApp.Platforms.Android.Services;

[Service(Enabled = true, Exported = false, ForegroundServiceType = Android.Content.PM.ForegroundService.TypeDataSync)]
public class ScheduleMonitorForegroundService : Service
{
    public const string ActionStart = "my.maui.notifier.schedule.START";
    public const string ActionStop = "my.maui.notifier.schedule.STOP";
    public const string SettingsExtraKey = "schedule_settings_json";

    private const string ChannelId = "schedule-monitor";
    private const int NotificationId = 9231;

    private readonly HttpClient _httpClient = new();
    private CancellationTokenSource? _cts;

    public override IBinder? OnBind(Intent? intent) => null;

    public override StartCommandResult OnStartCommand(Intent? intent, StartCommandFlags flags, int startId)
    {
        var action = intent?.Action;
        if (action == ActionStop)
        {
            StopMonitoring();
            StopForeground(StopForegroundFlags.Remove);
            StopSelf();
            return StartCommandResult.NotSticky;
        }

        var settingsJson = intent?.GetStringExtra(SettingsExtraKey);
        if (string.IsNullOrWhiteSpace(settingsJson))
        {
            StopSelf();
            return StartCommandResult.NotSticky;
        }

        var settings = JsonSerializer.Deserialize<ScheduleSettings>(settingsJson);
        if (settings is null)
        {
            StopSelf();
            return StartCommandResult.NotSticky;
        }

        StartForeground(NotificationId, BuildNotification("Schedule monitor is running"));
        StartMonitoring(settings);
        return StartCommandResult.Sticky;
    }

    private void StartMonitoring(ScheduleSettings settings)
    {
        StopMonitoring();
        _cts = new CancellationTokenSource();

        _ = Task.Run(async () =>
        {
            using var timer = new PeriodicTimer(TimeSpan.FromMinutes(Math.Max(1, settings.IntervalMinutes)));
            while (_cts is not null && await timer.WaitForNextTickAsync(_cts.Token))
            {
                if (!ScheduleMonitorEngine.WithinConfiguredWindow(DateTime.Now, settings))
                {
                    continue;
                }

                var result = await ScheduleMonitorEngine.CheckOnceAsync(_httpClient, settings, _cts.Token);
                if (result.HasAvailability)
                {
                    UpdateNotification("Availability found at endpoint");
                }
            }
        }, _cts.Token);
    }

    private void StopMonitoring()
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;
    }

    private Notification BuildNotification(string text)
    {
        var manager = (NotificationManager)GetSystemService(NotificationService)!;
        if (Build.VERSION.SdkInt >= BuildVersionCodes.O && manager.GetNotificationChannel(ChannelId) is null)
        {
            var channel = new NotificationChannel(ChannelId, "Schedule monitor", NotificationImportance.Low)
            {
                Description = "Background schedule monitoring"
            };
            manager.CreateNotificationChannel(channel);
        }

        var launchIntent = PackageManager?.GetLaunchIntentForPackage(PackageName) ?? new Intent(this, typeof(MainActivity));
        var pendingIntent = PendingIntent.GetActivity(this, 0, launchIntent, PendingIntentFlags.Immutable | PendingIntentFlags.UpdateCurrent);

        return new NotificationCompat.Builder(this, ChannelId)
            .SetContentTitle("My Maui Notifier")
            .SetContentText(text)
            .SetSmallIcon(Resource.Mipmap.appicon)
            .SetOngoing(true)
            .SetContentIntent(pendingIntent)
            .Build();
    }

    private void UpdateNotification(string text)
    {
        var manager = (NotificationManager)GetSystemService(NotificationService)!;
        manager.Notify(NotificationId, BuildNotification(text));
    }

    public override void OnDestroy()
    {
        StopMonitoring();
        base.OnDestroy();
    }
}
