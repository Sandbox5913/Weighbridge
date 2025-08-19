using Weighbridge.Pages;
using Weighbridge.Services;
using System.Diagnostics;

namespace Weighbridge
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            var services = Handler.MauiContext.Services;
            var userService = services.GetService<IUserService>();

            Page mainPage;

            // Check if user is already logged in
            if (userService?.CurrentUser != null)
            {
                // User is logged in, show the main app
                var appShell = services.GetService<AppShell>();
                Debug.WriteLine($"Setting MainPage to AppShell. HashCode: {appShell.GetHashCode()}");
                mainPage = appShell;
            }
            else
            {
                // User is not logged in, show login page
                var loginPage = services.GetService<LoginPage>();
                Debug.WriteLine($"Setting MainPage to LoginPage. HashCode: {loginPage.GetHashCode()}");
                mainPage = loginPage;
            }

            var window = new Window(mainPage);

#if WINDOWS
            window.Created += (s, e) =>
            {
                var nativeWindow = window.Handler.PlatformView as Microsoft.UI.Xaml.Window;
                if (nativeWindow != null)
                {
                    var appWindow = nativeWindow.AppWindow;
                    if (appWindow != null)
                    {
                        //appWindow.SetPresenter(Microsoft.UI.Windowing.AppWindowPresenterKind.FullScreen);
                        var presenter = appWindow.Presenter as Microsoft.UI.Windowing.OverlappedPresenter;
                        presenter?.Maximize();
                    }
                }
            };
#endif
            return window;
        }
    }
}