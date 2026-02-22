using Android.Content;
using MyMauiNotifierApp.Services;

namespace MyMauiNotifierApp;

public class ForegroundServiceManager : IForegroundServiceManager
{
    public bool IsRunning => ScheduleMonitorForegroundService.IsRunning;

    public void Start()
    {
        var context = Android.App.Application.Context;
        var intent = new Intent(context, typeof(ScheduleMonitorForegroundService));

        if (OperatingSystem.IsAndroidVersionAtLeast(26))
        {
            context.StartForegroundService(intent);
        }
        else
        {
            context.StartService(intent);
        }
    }

    public void Stop()
    {
        var context = Android.App.Application.Context;
        var intent = new Intent(context, typeof(ScheduleMonitorForegroundService));
        context.StopService(intent);
    }
}
