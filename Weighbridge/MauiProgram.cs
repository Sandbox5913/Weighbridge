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
using FluentValidation;
using Weighbridge.Validation;
using CommunityToolkit.Maui;

namespace Weighbridge;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        Debug.WriteLine("[MauiProgram] CreateMauiApp: Starting.");
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseMauiCommunityToolkit()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

#if DEBUG
        builder.Logging.AddDebug();
#endif

        // Add FluentValidation
        builder.Services.AddValidatorsFromAssemblyContaining<LoginValidator>();
        builder.Services.AddTransient<IValidator<Customer>, CustomerValidator>();
        builder.Services.AddTransient<IValidator<Driver>, DriverValidator>();
        builder.Services.AddTransient<IValidator<User>, UserValidator>();
        builder.Services.AddTransient<IValidator<Item>, ItemValidator>();
        builder.Services.AddTransient<IValidator<Site>, SiteValidator>();
        builder.Services.AddTransient<IValidator<Transport>, TransportValidator>();
        builder.Services.AddTransient<IValidator<Vehicle>, VehicleValidator>();
        builder.Services.AddTransient<IValidator<UserPageAccess>, UserPageAccessValidator>();

        // Register services
        builder.Services.AddSingleton<ILoggingService, LoggingService>();
        builder.Services.AddSingleton<IAlertService, AlertService>();
        builder.Services.AddSingleton<MainFormConfig>(); // Added
        builder.Services.AddSingleton<WeightParserService>();

        // Register Unit of Work and related services
        builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
        builder.Services.AddSingleton<IDbConnectionFactory, SqliteConnectionFactory>(); // Register IDbConnectionFactory

        builder.Services.AddScoped<IDatabaseService>(provider =>
        {
            // DatabaseService now gets its connection from the current UnitOfWork scope
            var unitOfWork = provider.GetRequiredService<IUnitOfWork>();
            return new DatabaseService(unitOfWork.Connection, provider);
        });
        builder.Services.AddScoped<IAuditLogRepository>(provider =>
        {
            var unitOfWork = provider.GetRequiredService<IUnitOfWork>();
            return new AuditLogRepository(unitOfWork.Connection);
        });
        builder.Services.AddSingleton<IWeighbridgeService, WeighbridgeService>();
        builder.Services.AddSingleton<IDocketService, DocketService>();
        builder.Services.AddSingleton<IReportsService, ReportsService>();
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
        builder.Services.AddSingleton<INavigationService, NavigationService>();
        builder.Services.AddSingleton<IExportService, ExportService>();
        builder.Services.AddScoped<IDocketValidationService, DocketValidationService>(); // Added
                builder.Services.AddScoped<IWeighingOperationService>(provider =>
        {
            return new WeighingOperationService(
                provider.GetRequiredService<IDatabaseService>(),
                provider.GetRequiredService<ILoggingService>(),
                provider.GetRequiredService<IAuditService>(),
                provider.GetRequiredService<IExportService>(),
                provider.GetRequiredService<IDocketService>(),
                provider.GetRequiredService<IWeighbridgeService>(),
                provider.GetRequiredService<IUnitOfWork>(),
                provider.GetRequiredService<IDocketValidationService>() // Added
            );
        });

        // Register ViewModels
        builder.Services.AddSingleton<MainPageViewModel>(provider =>
        {
            return new MainPageViewModel(
                provider.GetRequiredService<IWeighbridgeService>(),
                provider.GetRequiredService<IDatabaseService>(),
                provider.GetRequiredService<IDocketService>(),
                provider.GetRequiredService<IAuditService>(),
                provider.GetRequiredService<IExportService>(),
                provider.GetRequiredService<ILoggingService>(),
                provider.GetRequiredService<IAlertService>(),
                provider.GetRequiredService<IWeighingOperationService>()
            );
        });
        builder.Services.AddTransient<CustomerManagementViewModel>();
        builder.Services.AddTransient<DriverManagementViewModel>();
        builder.Services.AddTransient<LoginViewModel>();
        builder.Services.AddTransient<IValidator<LoginViewModel>, LoginValidator>();
        builder.Services.AddTransient<MainFormSettingsViewModel>();
        builder.Services.AddTransient<UserManagementViewModel>();
        builder.Services.AddTransient<UserPageAccessManagementViewModel>();
        builder.Services.AddTransient<SettingsViewModel>();
        builder.Services.AddTransient<AuditLogViewModel>();
        builder.Services.AddTransient<MaterialManagementViewModel>();
        builder.Services.AddTransient<SiteManagementViewModel>();
        builder.Services.AddTransient<TransportManagementViewModel>();
        builder.Services.AddTransient<VehicleManagementViewModel>();
        builder.Services.AddTransient<UserPageAccessManagementViewModel>();

        builder.Services.AddTransient<ReportsViewModel>();

        // Register Pages and inject ViewModels
        builder.Services.AddSingleton<MainPage>();
        builder.Services.AddSingleton<LoadsPage>(provider =>
        {
            return new LoadsPage(
                provider.GetRequiredService<IDatabaseService>(),
                provider.GetRequiredService<IDocketService>(),
                provider.GetRequiredService<IExportService>(),
                provider.GetRequiredService<IWeighbridgeService>()
            );
        });
        builder.Services.AddSingleton<SettingsPage>();
        builder.Services.AddSingleton<OutputSettingsPage>();
        builder.Services.AddTransient<EditLoadPage>();
        builder.Services.AddTransient<LoginPage>();
        builder.Services.AddSingleton<AppShell>();
        builder.Services.AddTransient<MainFormSettingsPage>();
        builder.Services.AddTransient<UserManagementPage>();
        builder.Services.AddTransient<UserPageAccessManagementPage>();
        builder.Services.AddTransient<AuditLogPage>();

        builder.Services.AddTransient<ReportsPage>();

        // Register Data Management Pages
        builder.Services.AddTransient<CustomerManagementPage>();
        builder.Services.AddTransient<DriverManagementViewModel>();
        builder.Services.AddTransient<MaterialManagementPage>();
        builder.Services.AddTransient<SiteManagementPage>();
        builder.Services.AddTransient<TransportManagementPage>();
        builder.Services.AddTransient<VehicleManagementPage>();

        builder.Services.AddSingleton<App>();

        Debug.WriteLine("[MauiProgram] CreateMauiApp: Building app.");
         
        var serviceProvider = builder.Services.BuildServiceProvider();
        var databaseService = serviceProvider.GetRequiredService<IDatabaseService>();
        databaseService.InitializeAsync().Wait(); // Blocking call to ensure initialization completes
        var app = builder.Build();
        
    
        Debug.WriteLine("[MauiProgram] CreateMauiApp: Returning app.");
        return app;
    }
}