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
            // Register services
            builder.Services.AddSingleton<WeighbridgeService>();

            // Register pages
            builder.Services.AddTransient<MainPage>();
            builder.Services.AddTransient<SettingsPage>();
            builder.Services.AddSingleton<DatabaseService>();
                        builder.Services.AddTransient<DataManagementPage>();

            // Also register any page that will use the service
            builder.Services.AddTransient<DataManagementPage>();
            return builder.Build();

           
        }
    }
}
