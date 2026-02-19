using MyMauiNotifierApp.Models;

namespace MyMauiNotifierApp.Services;

public class NoOpForegroundServiceController : IPlatformForegroundServiceController
{
    public bool IsSupported => false;

    public Task StartAsync(ScheduleSettings settings, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public void Stop()
    {
    }
}
