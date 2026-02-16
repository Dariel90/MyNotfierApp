using MyMauiNotifierApp.Models;

namespace MyMauiNotifierApp.Services;

public interface ISettingsStorage
{
    Task<ScheduleSettings> LoadAsync(CancellationToken cancellationToken = default);
    Task SaveAsync(ScheduleSettings settings, CancellationToken cancellationToken = default);
}
