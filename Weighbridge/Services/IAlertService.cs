using System.Threading.Tasks;

namespace Weighbridge.Services
{
    public interface IAlertService
    {
        Task DisplayAlert(string title, string message, string cancel);
        Task<bool> DisplayConfirmation(string title, string message, string accept, string cancel);
    }
}