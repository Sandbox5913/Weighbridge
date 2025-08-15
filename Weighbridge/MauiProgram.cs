using Microsoft.Extensions.Logging;
using Weighbridge.ViewModels;
using Weighbridge.Services;
using Weighbridge.Data;
using Weighbridge.Models;
using System.IO;
using Microsoft.Maui.Storage;
using Microsoft.Extensions.Logging.Debug; // Added
using Weighbridge.Pages; // Added
using SQLite; // Added for SQLiteAsyncConnection

namespace Weighbridge;

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
		builder.Logging.AddDebug();
#endif
		
		// Register SQLiteAsyncConnection
		builder.Services.AddSingleton<SQLiteAsyncConnection>(s =>
		{
			var dbPath = Path.Combine(FileSystem.AppDataDirectory, "weighbridge.db3");
			return new SQLiteAsyncConnection(dbPath);
		});

		// Register services
		builder.Services.AddSingleton<WeightParserService>();
		builder.Services.AddSingleton<IDatabaseService, DatabaseService>();
		builder.Services.AddSingleton<IWeighbridgeService, WeighbridgeService>();
		builder.Services.AddSingleton<IDocketService, DocketService>();
		builder.Services.AddSingleton<IPreviewService, PreviewService>(); // This should now work after PreviewService implements IPreviewService

		// Register ViewModels
		builder.Services.AddSingleton<MainPageViewModel>();

		// Register Pages and inject ViewModels
		builder.Services.AddSingleton<MainPage>();
		builder.Services.AddSingleton<LoadsPage>();
		builder.Services.AddSingleton<DataManagementPage>();
		builder.Services.AddSingleton<SettingsPage>();
		builder.Services.AddSingleton<PrintSettingsPage>();
		builder.Services.AddTransient<EditLoadPage>();


		return builder.Build();
	}
}
