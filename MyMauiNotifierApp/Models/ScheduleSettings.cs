namespace MyMauiNotifierApp.Models;

public class ScheduleSettings
{
    public string EndpointUrl { get; set; } = "https://citaprevia.ciencia.gob.es/qmaticwebbooking/rest/schedule/branches/5dd2e7c293b8a4ccb1b0683000f910198b9b75bbc94a8b2241ae7bea5adae1ac/dates;servicePublicId=51cc759c3ad62e040c5d2de4ab44280c643af620661ddb0dc3c9468c7d71a4d4;customSlotLength=100";
    public DateOnly StartDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);
    public DateOnly EndDate { get; set; } = DateOnly.FromDateTime(DateTime.Today.AddDays(30));
    public TimeOnly StartHour { get; set; } = new(8, 0);
    public TimeOnly EndHour { get; set; } = new(20, 0);
    public int IntervalMinutes { get; set; } = 15;
    public bool EnableVibrationToast { get; set; } = true;
    public bool EnableTelegramNotifications { get; set; }
    public string TelegramBotToken { get; set; } = string.Empty;
    public string TelegramChatId { get; set; } = string.Empty;
}
