using Android.Content;
using System.Text.Json;
using MyMauiNotifierApp.Models;
using MyMauiNotifierApp.Services;

namespace MyMauiNotifierApp.Platforms.Android.Services;

public class AndroidForegroundServiceController : IPlatformForegroundServiceController
{
    public bool IsSupported => true;

    public Task StartAsync(ScheduleSettings settings, CancellationToken cancellationToken = default)
    {
        var context = Android.App.Application.Context;
        var intent = new Intent(context, typeof(ScheduleMonitorForegroundService));
        intent.SetAction(ScheduleMonitorForegroundService.ActionStart);
        intent.PutExtra(ScheduleMonitorForegroundService.SettingsExtraKey, JsonSerializer.Serialize(settings));
        context.StartForegroundService(intent);
        return Task.CompletedTask;
    }

    public void Stop()
    {
        var context = Android.App.Application.Context;
        var intent = new Intent(context, typeof(ScheduleMonitorForegroundService));
        intent.SetAction(ScheduleMonitorForegroundService.ActionStop);
        context.StartService(intent);
    }
}
