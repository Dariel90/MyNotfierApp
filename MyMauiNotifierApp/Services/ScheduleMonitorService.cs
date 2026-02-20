using MyMauiNotifierApp.Models;

namespace MyMauiNotifierApp.Services;

public class ScheduleMonitorService : IScheduleMonitorService
{
    public ScheduleMonitorService(IHttpClientFactory httpClientFactory, IPlatformForegroundServiceController foregroundServiceController)
    {
        _httpClient = httpClientFactory.CreateClient();
        _foregroundServiceController = foregroundServiceController;
    }

    private readonly HttpClient _httpClient;
    private readonly IPlatformForegroundServiceController _foregroundServiceController;
    private PeriodicTimer? _timer;
    private CancellationTokenSource? _loopCts;

    public event Action<MonitorResult>? OnResult;
    public bool IsRunning { get; private set; }

    public async Task StartAsync(ScheduleSettings settings, CancellationToken cancellationToken = default)
    {
        await StopAsync();

        if (_foregroundServiceController.IsSupported)
        {
            await _foregroundServiceController.StartAsync(settings, cancellationToken);
            IsRunning = true;
            return;
        }

        _loopCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _timer = new PeriodicTimer(TimeSpan.FromMinutes(Math.Max(1, settings.IntervalMinutes)));
        IsRunning = true;

        _ = Task.Run(async () =>
        {
            while (_timer is not null && await _timer.WaitForNextTickAsync(_loopCts.Token))
            {
                var now = DateTime.Now;
                if (!ScheduleMonitorEngine.WithinConfiguredWindow(now, settings))
                {
                    continue;
                }

                var result = await CheckOnceAsync(settings, _loopCts.Token);
                OnResult?.Invoke(result);
            }
        }, _loopCts.Token);
    }

    public Task StopAsync()
    {
        if (_foregroundServiceController.IsSupported)
        {
            _foregroundServiceController.Stop();
        }

        _loopCts?.Cancel();
        _timer?.Dispose();
        _timer = null;
        _loopCts = null;
        IsRunning = false;
        return Task.CompletedTask;
    }

    public async Task<MonitorResult> CheckOnceAsync(ScheduleSettings settings, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync(settings.EndpointUrl, cancellationToken);
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync(cancellationToken);
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
}
