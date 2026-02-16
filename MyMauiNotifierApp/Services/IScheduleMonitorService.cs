using MyMauiNotifierApp.Models;

namespace MyMauiNotifierApp.Services;

public interface IScheduleMonitorService
{
    event Action<MonitorResult>? OnResult;
    bool IsRunning { get; }
    Task StartAsync(ScheduleSettings settings, CancellationToken cancellationToken = default);
    Task StopAsync();
    Task<MonitorResult> CheckOnceAsync(ScheduleSettings settings, CancellationToken cancellationToken = default);
}
