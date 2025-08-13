using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using System.Collections.Generic;

public class CustomerReceipt : IDocument
{
    private readonly string _logoPath;
    private readonly string _customerName;
    private readonly List<string> _fieldsToShow;
    private readonly float _pageWidthMm;
    private readonly float _pageHeightMm;

    public CustomerReceipt(
        string logoPath,
        string customerName,
        List<string> fieldsToShow,
        float pageWidthMm = 80,
        float pageHeightMm = 200)
    {
        _logoPath = logoPath;
        _customerName = customerName;
        _fieldsToShow = fieldsToShow;
        _pageWidthMm = pageWidthMm;
        _pageHeightMm = pageHeightMm;
    }

    public void Compose(IDocumentContainer container)
    {
        container.Page(page =>
        {
            float widthPoints = _pageWidthMm * 2.83465f;
            float heightPoints = _pageHeightMm * 2.83465f;

            page.Size(widthPoints, heightPoints);
            page.Margin(5);

            page.Header().Height(50).Row(row =>
            {
                row.RelativeItem().Text(_customerName).Bold().FontSize(12);
                if (!string.IsNullOrEmpty(_logoPath))
                    row.ConstantItem(50).Image(_logoPath).FitHeight();
            });

            page.Content().Column(column =>
            {
                foreach (var field in _fieldsToShow)
                    column.Item().Text(field).FontSize(10);

                column.Item().Text("Thank you!").FontSize(10).Italic();
            });
        });
    }
}
