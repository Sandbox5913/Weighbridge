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
            var window = new Window(Handler.MauiContext.Services.GetService<AppShell>());

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
