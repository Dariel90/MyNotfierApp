using Microsoft.Extensions.Logging;
using MudBlazor.Services;
#if ANDROID
using MyMauiNotifierApp.Platforms.Android.Services;
#endif
using MyMauiNotifierApp.Services;
using Plugin.LocalNotification;

namespace MyMauiNotifierApp
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                })
                .UseLocalNotification();

            builder.Services.AddMauiBlazorWebView();
            builder.Services.AddSingleton<IAlertNotificationService, AlertNotificationService>();
            builder.Services.AddSingleton<ISettingsStorage, SettingsStorage>();
#if ANDROID
            builder.Services.AddSingleton<IPlatformForegroundServiceController, AndroidForegroundServiceController>();
#else
            builder.Services.AddSingleton<IPlatformForegroundServiceController, NoOpForegroundServiceController>();
#endif
            builder.Services.AddSingleton<IScheduleMonitorService, ScheduleMonitorService>();

            builder.Services.AddHttpClient<ScheduleMonitorService>();

            builder.Services.AddMudServices();
#if DEBUG
            builder.Services.AddBlazorWebViewDeveloperTools();
            builder.Logging.AddDebug();
#endif
            var app = builder.Build();
            return app;
        }
    }
}
