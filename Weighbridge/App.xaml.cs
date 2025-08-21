using Weighbridge.Pages;
using Weighbridge.Services;
using System.Diagnostics;
using Microsoft.Maui.Controls;

namespace Weighbridge
{
    public partial class App : Application
    {
        public App(AppShell appShell) // Inject AppShell here
        {
            Debug.WriteLine("[App] App constructor: Starting.");
            InitializeComponent();
            Debug.WriteLine("[App] App constructor: After InitializeComponent.");
            MainPage = appShell; // Set MainPage directly
            Debug.WriteLine("[App] App constructor: MainPage set.");
        }
    }
}