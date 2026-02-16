using System.Text.Json;
using MyMauiNotifierApp.Models;

namespace MyMauiNotifierApp.Services;

public class SettingsStorage : ISettingsStorage
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private readonly string _settingsFilePath = Path.Combine(FileSystem.AppDataDirectory, "schedule-settings.json");

    public async Task<ScheduleSettings> LoadAsync(CancellationToken cancellationToken = default)
    {
        if (!File.Exists(_settingsFilePath))
        {
            return new ScheduleSettings();
        }

        var json = await File.ReadAllTextAsync(_settingsFilePath, cancellationToken);
        return JsonSerializer.Deserialize<ScheduleSettings>(json, JsonOptions) ?? new ScheduleSettings();
    }

    public async Task SaveAsync(ScheduleSettings settings, CancellationToken cancellationToken = default)
    {
        var json = JsonSerializer.Serialize(settings, JsonOptions);
        await File.WriteAllTextAsync(_settingsFilePath, json, cancellationToken);
    }
}
