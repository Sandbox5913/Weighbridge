using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Weighbridge.Models;
using System;
using System.IO;
using IContainer = QuestPDF.Infrastructure.IContainer;

namespace Weighbridge.Services
{
    public class DocketDocument : IDocument
    {
        private readonly DocketData _data;
        private readonly DocketTemplate _template;

        public DocketDocument(DocketData data, DocketTemplate template)
        {
            _data = data;
            _template = template;
        }

        public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

        public void Compose(IDocumentContainer container)
        {
            container
                .Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(50);

                    page.Header().Element(ComposeHeader);
                    page.Content().Element(ComposeContent);
                    page.Footer().AlignCenter().Text(x =>
                    {
                        x.Span("Page ");
                        x.CurrentPageNumber();
                    });
                });
        }

        void ComposeHeader(IContainer container)
        {
            var titleStyle = TextStyle.Default.FontSize(20).SemiBold().FontColor(QuestPDF.Helpers.Colors.Blue.Medium);

            container.Row(row =>
            {
                if (!string.IsNullOrEmpty(_template.LogoPath) && File.Exists(_template.LogoPath))
                {
                    var imageData = File.ReadAllBytes(_template.LogoPath);
                    row.RelativeItem().Image(imageData).FitArea();
                }

                row.RelativeItem().Column(column =>
                {
                    column.Item().Text("Weighbridge Docket").Style(titleStyle);
                    if (_template.ShowTimestamp)
                        column.Item().Text(_data.Timestamp.ToString("g"));
                });
            });
        }

        void ComposeContent(IContainer container)
        {
            container.PaddingVertical(40).Column(column =>
            {
                column.Spacing(5);

                if (_template.ShowVehicleLicense) column.Item().Row(row => AddRow(row, "Vehicle License:", _data.VehicleLicense));
                if (_template.ShowCustomer) column.Item().Row(row => AddRow(row, "Customer:", _data.Customer));
                if (_template.ShowTransportCompany) column.Item().Row(row => AddRow(row, "Transport Company:", _data.TransportCompany));
                if (_template.ShowDriver) column.Item().Row(row => AddRow(row, "Driver:", _data.Driver));
                if (_template.ShowSourceSite) column.Item().Row(row => AddRow(row, "Source Site:", _data.SourceSite));
                if (_template.ShowDestinationSite) column.Item().Row(row => AddRow(row, "Destination Site:", _data.DestinationSite));
                if (_template.ShowMaterial) column.Item().Row(row => AddRow(row, "Material:", _data.Material));
                if (_template.ShowEntranceWeight) column.Item().Row(row => AddRow(row, "Entrance Weight:", $"{_data.EntranceWeight} KG"));
                if (_template.ShowExitWeight) column.Item().Row(row => AddRow(row, "Exit Weight:", $"{_data.ExitWeight} KG"));
                if (_template.ShowNetWeight) column.Item().Row(row => AddRow(row, "Net Weight:", $"{_data.NetWeight} KG"));
                if (_template.ShowRemarks) column.Item().Row(row => AddRow(row, "Remarks:", _data.Remarks));
            });
        }

        void AddRow(RowDescriptor row, string label, string? value)
        {
            row.RelativeItem(1).Text(label).SemiBold();
            row.RelativeItem(2).Text(value ?? string.Empty);
        }
    }
}
