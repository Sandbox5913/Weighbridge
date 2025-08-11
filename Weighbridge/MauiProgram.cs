using Microsoft.Extensions.Logging;
using Weighbridge.Services;
using Weighbridge.Pages;
using Weighbridge.Data;

namespace Weighbridge
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
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

#if DEBUG
    		//builder.Logging.AddDebug();
#endif

            builder.Services.AddSingleton<App>();
            builder.Services.AddSingleton<AppShell>();
            builder.Services.AddSingleton<DatabaseService>();
            builder.Services.AddSingleton<WeighbridgeService>();
            builder.Services.AddTransient<SettingsPage>();
            builder.Services.AddTransient<MainPage>();

            return builder.Build();
        }
    }
}
