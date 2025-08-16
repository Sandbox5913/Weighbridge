using Weighbridge.Pages;
using Weighbridge.Services;

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
                mainPage = services.GetService<AppShell>();
            }
            else
            {
                // User is not logged in, show login page
                mainPage = services.GetService<LoginPage>();
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