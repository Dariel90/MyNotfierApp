# MyMauiNotifierApp

Lightweight .NET MAUI app that monitors an HTTP scheduling endpoint and notifies the user when availability is found. The UI is implemented with MudBlazor components inside a MAUI BlazorWebView.

## Features
- Periodic HTTP checks against a configurable endpoint
- Local schedule settings (date/time range, interval)
- In-app notification via Snackbar + optional vibration/toast
- Optional Telegram notifications (bot token + chat id)
- Manual "Check now" and start/stop controls

## Requirements
- .NET 10 SDK
- Visual Studio with .NET MAUI workload (or `dotnet` + MAUI support)
- Android SDK and emulator or physical device (targeting Android)

## Build & run
1. Open the solution in Visual Studio (or use `dotnet build`).
2. Select the Android project `MyMauiNotifierApp` as startup and choose an Android emulator or device.
3. Run (F5) to start a debug session.

Notes for debugging:
- Visual Studio may show many `open_from_bundles: failed to load bundled assembly` warnings when Fast Deployment (FastDev) is enabled. This is expected in debug sessions that use fast deployment.
- If you want a clean packaged APK (no FastDev), disable Fast Deployment / FastDev and Hot Reload in the project's Android options, then clean & rebuild.

To remove stale fast-deploy files from an Android device/emulator (if needed):
```
adb shell rm -rf /data/user/0/com.companyname.mymauinotifierapp/files/.__override__
```

## Configuration
Settings can be changed in the app Home page. The default configuration is stored in `MyMauiNotifierApp.Models.ScheduleSettings`.
Key settings:
- `EndpointUrl` — HTTP endpoint to poll
- `StartDate` / `EndDate` — date window
- `StartHour` / `EndHour` — time window
- `IntervalMinutes` — polling interval
- `EnableVibrationToast` — local notification behavior
- `EnableTelegramNotifications`, `TelegramBotToken`, `TelegramChatId`

## Permissions
If your app or embedded WebView requires Bluetooth on Android 12+, add runtime permissions for `BLUETOOTH_CONNECT` / `BLUETOOTH` in `Platforms/Android/AndroidManifest.xml` and request them at runtime. The debug logs may show WebView/Chromium warnings about missing Bluetooth permissions if the device/emulator tries to access Bluetooth.

## Known issues & debugging tips (from debug logs)
- `Setting NotFound and NotFoundPage properties simultaneously is not supported.`
  - The Blazor `Router` component is being configured with both `NotFound` and `NotFoundPage`. Use only one. Inspect `Components/Routes.razor` or wherever the `Router` is declared and remove either `NotFound` or `NotFoundPage`.

- `open_from_bundles: failed to load bundled assembly <X>.dll` warnings
  - Shown when Fast Deployment uploads assemblies separately. Not fatal in FastDev scenarios.

- UI jank / skipped frames (`Skipped 92 frames!`, `Davey! duration=...`) and long monitor contention messages
  - These indicate heavy work on the UI thread (startup or synchronous I/O). Move long-running operations off the main thread (use `Task.Run`, asynchronous I/O, background workers) and profile startup paths.

- WebView / Chromium messages like `BLUETOOTH_CONNECT permission is missing.` and `Failed to find entry 'classes.dex'`
  - WebView logs can be noisy — fix permissions if you need Bluetooth features; `classes.dex` messages during WebView initialization are usually not fatal.

- FastDev override directory messages (`.__override__`) and `Attempt to remove non-JNI local reference`
  - Typically benign for debug sessions but worth cleaning if you observe odd behavior.

## Project structure (important files)
- `Components/Pages/Home.razor` — main UI to configure and control the monitor
- `Models/ScheduleSettings.cs` — app settings model and defaults
- `Services/ScheduleMonitorService.cs` — core monitoring logic
- `Services/AlertNotificationService.cs` — notification delivery (vibration/toast/Telegram)
- `Services/SettingsStorage.cs` — persistence of settings
- `Platforms/Android/AndroidManifest.xml` — Android permissions and configuration

## Contributing
Contributions and bug reports are welcome. Open an issue or submit a pull request.

## License
This project follows the license in the repository (check `LICENSE` file if present).