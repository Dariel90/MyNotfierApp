using System.Net.Http.Json;
using Microsoft.Maui.Devices;
using MyMauiNotifierApp.Models;

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
            TryVibrate();
        }

        if (settings.EnableTelegramNotifications)
        {
            await SendTelegramMessageAsync(result, settings, cancellationToken);
        }
    }

    private static void TryVibrate()
    {
        try
        {
            Vibration.Default.Vibrate(TimeSpan.FromMilliseconds(500));
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

        var message = $"Availability detected at {result.CheckedAtUtc:u}. Preview: {result.ResponsePreview}";
        var endpoint = $"https://api.telegram.org/bot{settings.TelegramBotToken}/sendMessage";
        var payload = new
        {
            chat_id = settings.TelegramChatId,
            text = message
        };

        var response = await _httpClient.PostAsJsonAsync(endpoint, payload, cancellationToken);
        response.EnsureSuccessStatusCode();
    }
}
