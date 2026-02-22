using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using AndroidX.Core.App;
using MyMauiNotifierApp.Models;
using MyMauiNotifierApp.Services;
using Plugin.LocalNotification;
using System.Net.Http.Json;
using System.Text.RegularExpressions;

namespace MyMauiNotifierApp;

[Service(ForegroundServiceType = ForegroundService.TypeDataSync, Exported = false)]
public class ScheduleMonitorForegroundService : Android.App.Service
{
    private const string ChannelId = "schedule_monitor_channel";
    private const string ChannelName = "Schedule Monitor";
    private const int ForegroundNotificationId = 9000;

    private CancellationTokenSource? _cts;
    private PeriodicTimer? _timer;
    private HttpClient? _httpClient;

    public static bool IsRunning { get; private set; }

    public override IBinder? OnBind(Intent? intent) => null;

    public override StartCommandResult OnStartCommand(Intent? intent, StartCommandFlags flags, int startId)
    {
        CreateNotificationChannel();
        var notification = BuildNotification("Schedule monitor is starting...");

        if (OperatingSystem.IsAndroidVersionAtLeast(29))
        {
            StartForeground(ForegroundNotificationId, notification, ForegroundService.TypeDataSync);
        }
        else
        {
            StartForeground(ForegroundNotificationId, notification);
        }

        IsRunning = true;
        _httpClient = new HttpClient();
        _cts = new CancellationTokenSource();
        _ = RunMonitorLoopAsync(_cts.Token);

        return StartCommandResult.Sticky;
    }

    public override void OnDestroy()
    {
        IsRunning = false;
        _cts?.Cancel();
        _timer?.Dispose();
        _timer = null;
        _cts?.Dispose();
        _cts = null;
        _httpClient?.Dispose();
        _httpClient = null;
        base.OnDestroy();
    }

    private async Task RunMonitorLoopAsync(CancellationToken token)
    {
        try
        {
            var settingsStorage = new SettingsStorage();
            var settings = await settingsStorage.LoadAsync(token);

            _timer = new PeriodicTimer(TimeSpan.FromMinutes(Math.Max(1, settings.IntervalMinutes)));
            UpdateNotification("Schedule monitor is running.");

            while (await _timer.WaitForNextTickAsync(token))
            {
                settings = await settingsStorage.LoadAsync(token);

                var now = DateTime.Now;
                if (!WithinConfiguredWindow(now, settings))
                {
                    UpdateNotification($"Outside monitoring window. Next window starts at {settings.StartHour}.");
                    continue;
                }

                var result = await CheckEndpointAsync(settings, token);

                if (result.HasAvailability)
                {
                    await HandleAvailabilityAsync(result, settings, token);
                }

                UpdateNotification($"Last check: {result.CheckedAt:HH:mm:ss} – {result.Message}");
            }
        }
        catch (System.OperationCanceledException ex)
        {
            // Expected when the service is stopped.
        }
        catch (Exception ex)
        {
            UpdateNotification($"Monitor error: {ex.Message}");
        }
    }

    private async Task<MonitorResult> CheckEndpointAsync(ScheduleSettings settings, CancellationToken token)
    {
        try
        {
            var response = await _httpClient!.GetAsync(settings.EndpointUrl, token);
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync(token);
            var hasAvailability = LooksLikeNonEmptyAvailability(content);

            return new MonitorResult
            {
                CheckedAt = DateTime.Now,
                HasAvailability = hasAvailability,
                Message = hasAvailability
                    ? "Availability detected in endpoint response."
                    : "No availability found in endpoint response.",
                ResponsePreview = content.Length > 250 ? content[..250] : content
            };
        }
        catch (Exception ex)
        {
            return new MonitorResult
            {
                CheckedAt = DateTime.Now,
                HasAvailability = false,
                Message = $"Error while checking schedule: {ex.Message}",
                ResponsePreview = string.Empty
            };
        }
    }

    private async Task HandleAvailabilityAsync(MonitorResult result, ScheduleSettings settings, CancellationToken token)
    {
        if (settings.EnableVibrationToast)
        {
            try
            {
                Vibration.Default.Vibrate(TimeSpan.FromMilliseconds(5000));
            }
            catch
            {
                // Device may not support vibration.
            }

            var request = new NotificationRequest
            {
                NotificationId = 1001,
                Title = "Availability Detected",
                Description = "An availability was detected. Check your Telegram for details.",
                Schedule = new NotificationRequestSchedule
                {
                    NotifyTime = DateTime.Now.AddSeconds(1)
                }
            };
            await LocalNotificationCenter.Current.Show(request);
        }

        if (settings.EnableTelegramNotifications
            && !string.IsNullOrWhiteSpace(settings.TelegramBotToken)
            && !string.IsNullOrWhiteSpace(settings.TelegramChatId))
        {
            try
            {
                var message = $"Availability detected at {result.CheckedAt}. Preview: {result.ResponsePreview}";
                var endpoint = $"https://api.telegram.org/bot{settings.TelegramBotToken}/sendMessage";
                var payload = new { chat_id = settings.TelegramChatId, text = message };
                await _httpClient!.PostAsJsonAsync(endpoint, payload, token);
            }
            catch
            {
                // Telegram notification failed; continue monitoring.
            }
        }
    }

    private static bool WithinConfiguredWindow(DateTime now, ScheduleSettings settings)
    {
        var nowDate = DateOnly.FromDateTime(now);
        var nowTime = TimeOnly.FromDateTime(now);
        return nowDate >= settings.StartDate
               && nowDate <= settings.EndDate
               && nowTime >= settings.StartHour
               && nowTime <= settings.EndHour;
    }

    private static bool LooksLikeNonEmptyAvailability(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return false;
        }

        var compact = content.Trim();
        if (compact is "[]" or "{}" or "null")
        {
            return false;
        }

        return Regex.IsMatch(compact, "\\d{4}-\\d{2}-\\d{2}") || compact.Length > 10;
    }

    private void CreateNotificationChannel()
    {
        if (!OperatingSystem.IsAndroidVersionAtLeast(26))
        {
            return;
        }

        var channel = new NotificationChannel(ChannelId, ChannelName, NotificationImportance.Low)
        {
            Description = "Shows the schedule monitor status"
        };
        var manager = (NotificationManager?)GetSystemService(NotificationService);
        manager?.CreateNotificationChannel(channel);
    }

    private Notification BuildNotification(string text)
    {
        var pendingIntent = PendingIntent.GetActivity(
            this,
            0,
            new Intent(this, typeof(MainActivity)),
            PendingIntentFlags.Immutable);

        return new NotificationCompat.Builder(this, ChannelId)
            .SetContentTitle("Schedule Monitor")
            .SetContentText(text)
            .SetSmallIcon(Resource.Mipmap.appicon)
            .SetContentIntent(pendingIntent)
            .SetOngoing(true)
            .Build();
    }

    private void UpdateNotification(string text)
    {
        var notification = BuildNotification(text);
        var manager = (NotificationManager?)GetSystemService(NotificationService);
        manager?.Notify(ForegroundNotificationId, notification);
    }
}
