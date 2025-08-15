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
            builder.Services.AddSingleton<IDatabaseService, DatabaseService>();
            builder.Services.AddSingleton<WeightParserService>(); // Must be registered before WeighbridgeService
            builder.Services.AddSingleton<IWeighbridgeService, WeighbridgeService>();
            builder.Services.AddSingleton<IDocketService, DocketService>();

            // Register pages
            builder.Services.AddTransient<MainPage>();
            builder.Services.AddTransient<SettingsPage>();
            builder.Services.AddTransient<DataManagementPage>();
            builder.Services.AddTransient<PrintSettingsPage>();
            builder.Services.AddTransient<LoadsPage>();

            return builder.Build();
        }
    }
}