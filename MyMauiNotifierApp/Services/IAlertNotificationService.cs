using MyMauiNotifierApp.Models;

namespace MyMauiNotifierApp.Services;

public interface IAlertNotificationService
{
    Task NotifyAvailabilityAsync(MonitorResult result, ScheduleSettings settings, CancellationToken cancellationToken = default);
}
