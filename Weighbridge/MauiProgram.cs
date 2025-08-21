using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Debug;
using Microsoft.Maui.LifecycleEvents;
using Microsoft.Maui.Storage;
using SQLite; // Still needed for some models, but not for connection directly
using System.Data; // Added for IDbConnection
using Microsoft.Data.Sqlite; // Added for SqliteConnection
using System.Diagnostics;
using System.IO;
using Weighbridge.Data;
using Weighbridge.Models;
using Weighbridge.Pages;
using Weighbridge.Services;
using Weighbridge.ViewModels;

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

        // Register services
        builder.Services.AddSingleton<WeightParserService>();

        // Register a single IDbConnection for the application
        builder.Services.AddSingleton<IDbConnection>(provider =>
        {
            // Use a file-based SQLite database for the main application
            var databasePath = Path.Combine(FileSystem.AppDataDirectory, "weighbridge.db");
            var connection = new SqliteConnection($"Data Source={databasePath}");
            connection.Open(); // Open the connection once
            return connection;
        });

        builder.Services.AddSingleton<IDatabaseService>(provider =>
        {
            var dbConnection = provider.GetRequiredService<IDbConnection>();
            return new DatabaseService(dbConnection, provider); // Pass the IDbConnection directly
        });
        builder.Services.AddSingleton<IDbConnectionFactory, SqliteConnectionFactory>(); // Register IDbConnectionFactory
        builder.Services.AddSingleton<IAuditLogRepository, AuditLogRepository>();
        builder.Services.AddSingleton<IWeighbridgeService, WeighbridgeService>();
        builder.Services.AddSingleton<IDocketService, DocketService>();
        builder.Services.AddSingleton<IPreviewService, PreviewService>();
        builder.Services.AddSingleton<IAuditService, AuditService>(provider =>
        {
            var auditLogRepo = provider.GetRequiredService<IAuditLogRepository>();
            var userService = provider.GetRequiredService<IUserService>();
            return new AuditService(auditLogRepo, () => userService.CurrentUser);
        });
        builder.Services.AddSingleton<IUserService, UserService>(provider =>
        {
            var dbService = provider.GetRequiredService<IDatabaseService>();
            return new UserService(dbService, provider); // Pass the provider itself
        });
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
        builder.Services.AddTransient<AuditLogViewModel>();

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
        builder.Services.AddTransient<AuditLogPage>();

        // Register Data Management Pages
        builder.Services.AddTransient<CustomerManagementPage>();
        builder.Services.AddTransient<DriverManagementViewModel>();
        builder.Services.AddTransient<MaterialManagementPage>();
        builder.Services.AddTransient<SiteManagementPage>();
        builder.Services.AddTransient<TransportManagementPage>();
        builder.Services.AddTransient<VehicleManagementPage>();

        builder.Services.AddSingleton<App>();

        Debug.WriteLine("[MauiProgram] CreateMauiApp: Building app.");
         builder.UseMauiApp<App>()
     .ConfigureLifecycleEvents(events =>
     {
#if WINDOWS
         events.AddWindows(windows =>
         {
             windows.OnWindowCreated((window) =>
             {
                 var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(window);

                 var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hwnd);
                 var appWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(windowId);

                 appWindow.SetPresenter(Microsoft.UI.Windowing.AppWindowPresenterKind.Default);
             });
         });
#endif
     });
        var app = builder.Build();
        
    
        Debug.WriteLine("[MauiProgram] CreateMauiApp: Returning app.");
        return app;
    }
}