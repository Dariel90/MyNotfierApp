using MyMauiNotifierApp.Models;
using Plugin.LocalNotification;
using System.Net.Http.Json;

namespace MyMauiNotifierApp.Services;

public class AlertNotificationService(HttpClient httpClient) : IAlertNotificationService
{
    private readonly HttpClient _httpClient = httpClient;

    public async Task NotifyAvailabilityAsync(MonitorResult result, ScheduleSettings settings, CancellationToken cancellationToken = default)
    {
        if (!result.HasAvailability)
        {
            return;
        }

        if (settings.EnableVibrationToast)
        {
            TryVibrateAndPushNotification();
        }

        if (settings.EnableTelegramNotifications)
        {
            await SendTelegramMessageAsync(result, settings, cancellationToken);
        }
    }

    private async void TryVibrateAndPushNotification()
    {
        try
        {
            Vibration.Default.Vibrate(TimeSpan.FromMilliseconds(5000));
            await ShowNotificationAsync(
                id: 1001,
                title: "Availability Detected",
                description: "An availability was detected. Check your Telegram for details.",
                scheduleSeconds: 1);
        }
        catch
        {
            // Device does not support vibration or permission is missing.
        }
    }

    private async Task SendTelegramMessageAsync(MonitorResult result, ScheduleSettings settings, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(settings.TelegramBotToken) || string.IsNullOrWhiteSpace(settings.TelegramChatId))
        {
            return;
        }

        var message = $"Availability detected at {result.CheckedAt:u}. Preview: {result.ResponsePreview}";
        var endpoint = $"https://api.telegram.org/bot{settings.TelegramBotToken}/sendMessage";
        var payload = new
        {
            chat_id = settings.TelegramChatId,
            text = message
        };

        var response = await _httpClient.PostAsJsonAsync(endpoint, payload, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    public async Task ShowNotificationAsync(
        int id,
        string title,
        string description,
        int scheduleSeconds = 1,
        string returningData = "")
    {
        var request = new NotificationRequest
        {
            NotificationId = id,
            Title = title,
            Description = description,
            ReturningData = returningData,
            Schedule = new NotificationRequestSchedule
            {
                NotifyTime = DateTime.Now.AddSeconds(scheduleSeconds)
            }
        };

        await LocalNotificationCenter.Current.Show(request);
    }

}
