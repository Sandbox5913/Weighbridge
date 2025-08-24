using System.Threading.Tasks;
using Microsoft.Maui.Controls;

namespace Weighbridge.Services
{
    public class AlertService : IAlertService
    {
        public Task DisplayAlert(string title, string message, string cancel)
        {
            return Application.Current.MainPage.DisplayAlert(title, message, cancel);
        }

        public Task<bool> DisplayConfirmation(string title, string message, string accept, string cancel)
        {
            return Application.Current.MainPage.DisplayAlert(title, message, accept, cancel);
        }
    }
}