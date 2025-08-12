using Weighbridge.Data; // Add this using directive

namespace Weighbridge
{
    public partial class App : Application
    {
        public App(DatabaseService dbService) // The service is provided automatically
        {
            InitializeComponent();

            // Initialize the database when the app starts
            dbService.InitializeAsync().Wait();

            MainPage = new AppShell();
        }


    }
}