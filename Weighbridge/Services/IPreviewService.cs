using Microsoft.Maui.Controls;
using System.Threading.Tasks;
using Weighbridge.Models;

namespace Weighbridge.Services
{
    public interface IPreviewService
    {
        Task<ImageSource> GeneratePreviewAsync(DocketData data, DocketTemplate template);
    }
}
