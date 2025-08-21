using Microsoft.Extensions.Logging;
using Weighbridge.ViewModels;
using Weighbridge.Services;
using Weighbridge.Data;
using Weighbridge.Models;
using System.IO;
using Microsoft.Maui.Storage;
using Microsoft.Extensions.Logging.Debug;
using Weighbridge.Pages;
using SQLite; // Still needed for some models, but not for connection directly
using System.Data; // Added for IDbConnection
using System.Diagnostics;

namespace Weighbridge;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        Debug.WriteLine("[MauiProgram] CreateMauiApp: Starting.");
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

#if DEBUG
        builder.Logging.AddDebug();
#endif

        // Register IDbConnectionFactory
        builder.Services.AddSingleton<IDbConnectionFactory, SqliteConnectionFactory>();

        // Register services
        builder.Services.AddSingleton<WeightParserService>();
        builder.Services.AddSingleton<IDatabaseService, DatabaseService>();
        builder.Services.AddSingleton<IAuditLogRepository, AuditLogRepository>(); // DatabaseService now takes IDbConnectionFactory
        builder.Services.AddSingleton<IWeighbridgeService, WeighbridgeService>();
        builder.Services.AddSingleton<IDocketService, DocketService>();
        builder.Services.AddSingleton<IPreviewService, PreviewService>();
        builder.Services.AddSingleton<IAuditService, AuditService>(provider =>
        {
            var auditLogRepo = provider.GetRequiredService<IAuditLogRepository>();
            var userService = provider.GetRequiredService<IUserService>();
            return new AuditService(auditLogRepo, () => userService.CurrentUser);
        });
        builder.Services.AddSingleton<IUserService, UserService>();
        builder.Services.AddTransient<INavigationService, NavigationService>();

        // Register ViewModels
        builder.Services.AddSingleton<MainPageViewModel>();
        builder.Services.AddTransient<CustomerManagementViewModel>();
        builder.Services.AddTransient<DriverManagementViewModel>();
        builder.Services.AddTransient<LoginViewModel>();
        builder.Services.AddTransient<MainFormSettingsViewModel>();
        builder.Services.AddTransient<UserManagementViewModel>();
        builder.Services.AddTransient<UserPageAccessManagementViewModel>();
        builder.Services.AddTransient<SettingsViewModel>();

        // Register Pages and inject ViewModels
        builder.Services.AddSingleton<MainPage>();
        builder.Services.AddSingleton<LoadsPage>();
        builder.Services.AddSingleton<SettingsPage>();
        builder.Services.AddSingleton<PrintSettingsPage>();
        builder.Services.AddTransient<EditLoadPage>();
        builder.Services.AddTransient<LoginPage>();
        builder.Services.AddSingleton<AppShell>();
        builder.Services.AddTransient<MainFormSettingsPage>();
        builder.Services.AddTransient<UserManagementPage>();
        builder.Services.AddTransient<UserPageAccessManagementPage>();

        // Register Data Management Pages
        builder.Services.AddTransient<CustomerManagementPage>();
        builder.Services.AddTransient<DriverManagementViewModel>();
        builder.Services.AddTransient<MaterialManagementPage>();
        builder.Services.AddTransient<SiteManagementPage>();
        builder.Services.AddTransient<TransportManagementPage>();
        builder.Services.AddTransient<VehicleManagementPage>();

        builder.Services.AddSingleton<App>();

        Debug.WriteLine("[MauiProgram] CreateMauiApp: Building app.");
        var app = builder.Build();

        Debug.WriteLine("[MauiProgram] CreateMauiApp: Returning app.");
        return app;
    }
}