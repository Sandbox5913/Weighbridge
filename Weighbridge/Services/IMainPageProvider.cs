using System.Threading.Tasks;
using Microsoft.Maui.Controls;

namespace Weighbridge.Services
{
    public interface IMainPageProvider
    {
        Task DisplayAlert(string title, string message, string cancel);
        Task<bool> DisplayAlert(string title, string message, string accept, string cancel);
    }
}