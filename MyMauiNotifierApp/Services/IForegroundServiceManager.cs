namespace MyMauiNotifierApp.Services;

public interface IForegroundServiceManager
{
    bool IsRunning { get; }
    void Start();
    void Stop();
}
