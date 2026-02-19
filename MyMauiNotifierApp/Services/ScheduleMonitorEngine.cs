using System.Text.RegularExpressions;
using MyMauiNotifierApp.Models;

namespace MyMauiNotifierApp.Services;

public static class ScheduleMonitorEngine
{
    public static async Task<MonitorResult> CheckOnceAsync(HttpClient httpClient, ScheduleSettings settings, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await httpClient.GetAsync(settings.EndpointUrl, cancellationToken);
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            var hasAvailability = LooksLikeNonEmptyAvailability(content);

            return new MonitorResult
            {
                CheckedAtUtc = DateTime.UtcNow,
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
                CheckedAtUtc = DateTime.UtcNow,
                HasAvailability = false,
                Message = $"Error while checking schedule: {ex.Message}",
                ResponsePreview = string.Empty
            };
        }
    }

    public static bool WithinConfiguredWindow(DateTime now, ScheduleSettings settings)
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
}
