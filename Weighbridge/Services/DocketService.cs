using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using Weighbridge.Models;
using System.IO;
using System.Threading.Tasks;
using Weighbridge.Data;

namespace Weighbridge.Services
{
    public class DocketService : IDocketService
    {
        private readonly IDatabaseService _databaseService;
        private readonly IAuditService _auditService;

        public DocketService(IDatabaseService databaseService, IAuditService auditService)
        {
            _databaseService = databaseService;
            _auditService = auditService;
        }
        public async Task<string> GeneratePdfAsync(DocketData data, DocketTemplate template)
        {
            var document = new DocketDocument(data, template);
            var filePath = Path.Combine(FileSystem.CacheDirectory, $"Docket_{DateTime.Now:yyyyMMddHHmmss}.pdf");

            await Task.Run(() => document.GeneratePdf(filePath));

            return filePath;
        }

        public async Task<Stream> GeneratePdfToStreamAsync(DocketData data, DocketTemplate template)
        {
            var document = new DocketDocument(data, template);
            var stream = new MemoryStream();
            await Task.Run(() => document.GeneratePdf(stream));
            stream.Position = 0;
            return stream;
        }

        public async Task CancelDocket(int docketId)
        {
            var docket = await _databaseService.GetItemAsync<Docket>(docketId);
            if (docket != null)
            {
                docket.Status = "CANCELLED"; // Or an appropriate cancelled status
                await _databaseService.SaveItemAsync(docket);
                await _auditService.LogActionAsync("Cancelled", "Docket", docket.Id, $"Docket {docket.Id} cancelled.");
            }
        }
    }
}