
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Weighbridge.Models;

namespace Weighbridge.Services
{
    public class ExportService : IExportService
    {
        private readonly IDatabaseService _databaseService;

        public ExportService(IDatabaseService databaseService)
        {
            _databaseService = databaseService;
        }

        public async Task ExportDocketAsync(Docket docket, WeighbridgeConfig config)
        {
            if (docket == null || config == null || !config.ExportEnabled)
                return;

            var exportFormat = config.ExportFormat.ToLower();
            var fileName = $"Docket_{docket.Id}_{docket.Timestamp:yyyyMMddHHmmss}.{exportFormat}";
            var filePath = Path.Combine(config.ExportFolderPath, fileName);

            string fileContent = string.Empty;
            if (exportFormat == "csv")
            {
                fileContent = await GenerateCsvAsync(docket);
            }
            else if (exportFormat == "xml")
            {
                fileContent = await GenerateXmlAsync(docket);
            }

            if (!string.IsNullOrEmpty(fileContent))
            {
                await File.WriteAllTextAsync(filePath, fileContent);
            }
        }

        private async Task<string> GenerateCsvAsync(Docket docket)
        {
            var vehicle = await _databaseService.GetItemAsync<Vehicle>(docket.VehicleId.GetValueOrDefault());
            var sourceSite = docket.SourceSiteId.HasValue ? await _databaseService.GetItemAsync<Site>(docket.SourceSiteId.Value) : null;
            var destinationSite = docket.DestinationSiteId.HasValue ? await _databaseService.GetItemAsync<Site>(docket.DestinationSiteId.Value) : null;
            var item = docket.ItemId.HasValue ? await _databaseService.GetItemAsync<Item>(docket.ItemId.Value) : null;
            var customer = docket.CustomerId.HasValue ? await _databaseService.GetItemAsync<Customer>(docket.CustomerId.Value) : null;
            var transport = docket.TransportId.HasValue ? await _databaseService.GetItemAsync<Transport>(docket.TransportId.Value) : null;
            var driver = docket.DriverId.HasValue ? await _databaseService.GetItemAsync<Driver>(docket.DriverId.Value) : null;

            var csv = new StringBuilder();
            csv.AppendLine("Docket ID,Timestamp,Status,Vehicle,Entrance Weight,Exit Weight,Net Weight,Source,Destination,Item,Customer,Transport,Driver,Remarks");
            csv.AppendLine($"{docket.Id},{docket.Timestamp},{docket.Status},{vehicle?.LicenseNumber},{docket.EntranceWeight},{docket.ExitWeight},{docket.NetWeight},{sourceSite?.Name},{destinationSite?.Name},{item?.Name},{customer?.Name},{transport?.Name},{driver?.Name},{docket.Remarks}");

            return csv.ToString();
        }

        private async Task<string> GenerateXmlAsync(Docket docket)
        {
            var vehicle = await _databaseService.GetItemAsync<Vehicle>(docket.VehicleId.GetValueOrDefault());
            var sourceSite = docket.SourceSiteId.HasValue ? await _databaseService.GetItemAsync<Site>(docket.SourceSiteId.Value) : null;
            var destinationSite = docket.DestinationSiteId.HasValue ? await _databaseService.GetItemAsync<Site>(docket.DestinationSiteId.Value) : null;
            var item = docket.ItemId.HasValue ? await _databaseService.GetItemAsync<Item>(docket.ItemId.Value) : null;
            var customer = docket.CustomerId.HasValue ? await _databaseService.GetItemAsync<Customer>(docket.CustomerId.Value) : null;
            var transport = docket.TransportId.HasValue ? await _databaseService.GetItemAsync<Transport>(docket.TransportId.Value) : null;
            var driver = docket.DriverId.HasValue ? await _databaseService.GetItemAsync<Driver>(docket.DriverId.Value) : null;

            var docketViewModel = new DocketViewModel
            {
                Id = docket.Id,
                EntranceWeight = docket.EntranceWeight,
                ExitWeight = docket.ExitWeight,
                NetWeight = docket.NetWeight,
                Timestamp = docket.Timestamp,
                Status = docket.Status,
                Remarks = docket.Remarks,
                VehicleLicense = vehicle?.LicenseNumber,
                SourceSiteName = sourceSite?.Name,
                DestinationSiteName = destinationSite?.Name,
                ItemName = item?.Name,
                CustomerName = customer?.Name,
                TransportName = transport?.Name,
                DriverName = driver?.Name,
                HasRemarks = !string.IsNullOrWhiteSpace(docket.Remarks)
            };

            var serializer = new XmlSerializer(typeof(DocketViewModel));
            using (var writer = new StringWriter())
            {
                serializer.Serialize(writer, docketViewModel);
                return writer.ToString();
            }
        }
    }
}
