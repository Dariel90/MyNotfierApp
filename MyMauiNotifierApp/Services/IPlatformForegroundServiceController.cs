using MyMauiNotifierApp.Models;

namespace MyMauiNotifierApp.Services;

public interface IPlatformForegroundServiceController
{
    bool IsSupported { get; }
    Task StartAsync(ScheduleSettings settings, CancellationToken cancellationToken = default);
    void Stop();
}
