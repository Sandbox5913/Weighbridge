using System.Threading.Tasks;
using Microsoft.Maui.Controls;

namespace Weighbridge.Services
{
    public class AlertService : IAlertService
    {
        private readonly IMainPageProvider _mainPageProvider;

        public AlertService(IMainPageProvider mainPageProvider)
        {
            _mainPageProvider = mainPageProvider;
        }

        public Task DisplayAlert(string title, string message, string cancel)
        {
            return _mainPageProvider.DisplayAlert(title, message, cancel);
        }

        public Task<bool> DisplayConfirmation(string title, string message, string accept, string cancel)
        {
            return _mainPageProvider.DisplayAlert(title, message, accept, cancel);
        }
    }

    public class MainPageProvider : IMainPageProvider
    {
        public Task DisplayAlert(string title, string message, string cancel)
        {
            return Application.Current.MainPage.DisplayAlert(title, message, cancel);
        }

        public Task<bool> DisplayAlert(string title, string message, string accept, string cancel)
        {
            return Application.Current.MainPage.DisplayAlert(title, message, accept, cancel);
        }
    }
}