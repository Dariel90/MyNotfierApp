using Microsoft.Extensions.Logging;
using MudBlazor.Services;
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
                .UseLocalNotification();   // <-- ADD THIS           

            builder.Services.AddMauiBlazorWebView();
            builder.Services.AddSingleton<IAlertNotificationService, AlertNotificationService>();
            builder.Services.AddSingleton<ISettingsStorage, SettingsStorage>();
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
