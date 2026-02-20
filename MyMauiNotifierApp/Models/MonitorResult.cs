namespace MyMauiNotifierApp.Models;

public class MonitorResult
{
    public DateTime CheckedAt { get; init; }
    public bool HasAvailability { get; init; }
    public string Message { get; init; } = string.Empty;
    public string ResponsePreview { get; init; } = string.Empty;
}
