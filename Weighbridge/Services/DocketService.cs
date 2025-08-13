using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using Weighbridge.Models;
using System.IO;
using System.Threading.Tasks;

namespace Weighbridge.Services
{
    public class DocketService
    {
        public async Task<string> GeneratePdfAsync(DocketData data, DocketTemplate template)
        {
            var document = new DocketDocument(data, template);
            var filePath = Path.Combine(FileSystem.CacheDirectory, $"Docket_{DateTime.Now:yyyyMMddHHmmss}.pdf");

            await Task.Run(() => document.GeneratePdf(filePath));

            return filePath;
        }
    }
}
