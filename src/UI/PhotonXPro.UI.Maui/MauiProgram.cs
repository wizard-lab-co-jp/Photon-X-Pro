using Microsoft.Extensions.Logging;
using Microsoft.Maui.LifecycleEvents;

namespace PhotonXPro.UI.Maui;

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
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
			})
            .ConfigureLifecycleEvents(events =>
            {
#if WINDOWS
                events.AddWindows(windowsLifecycleBuilder =>
                {
                    windowsLifecycleBuilder.OnWindowCreated(window =>
                    {
                        window.ExtendsContentIntoTitleBar = true;
                        var handle = WinRT.Interop.WindowNative.GetWindowHandle(window);
                        var id = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(handle);
                        var appWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(id);

                        appWindow.TitleBar.ButtonBackgroundColor = Microsoft.UI.Colors.Transparent;
                        appWindow.TitleBar.ButtonForegroundColor = Microsoft.UI.Colors.White;
                        appWindow.TitleBar.ButtonHoverBackgroundColor = Microsoft.UI.ColorHelper.FromArgb(255, 51, 51, 51);
                        appWindow.TitleBar.ButtonPressedBackgroundColor = Microsoft.UI.ColorHelper.FromArgb(255, 68, 68, 68);
                        appWindow.TitleBar.ButtonInactiveBackgroundColor = Microsoft.UI.Colors.Transparent;
                        appWindow.TitleBar.ButtonInactiveForegroundColor = Microsoft.UI.Colors.Gray;
                    });
                });
#endif
            });

#if DEBUG
		builder.Logging.AddDebug();
#endif

		return builder.Build();
	}
}
