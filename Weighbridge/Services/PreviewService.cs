using PDFiumSharp;
using System.IO;
using System.Threading.Tasks;
using Weighbridge.Models;
using Weighbridge.Services;

namespace Weighbridge.Services
{
    public class PreviewService
    {
        private readonly DocketService _docketService;

        public PreviewService(DocketService docketService)
        {
            _docketService = docketService;
        }

        public async Task<ImageSource> GeneratePreviewAsync(DocketData data, DocketTemplate template)
        {
            using (var pdfStream = await _docketService.GeneratePdfToStreamAsync(data, template) as MemoryStream)
            {
                if (pdfStream == null)
                {
                    return null;
                }

                var pdfBytes = pdfStream.ToArray();

                using (var pdfDocument = new PdfDocument(pdfBytes))
                {
                    var page = pdfDocument.Pages[0];
                    using (var bitmap = new PDFiumBitmap((int)page.Width, (int)page.Height, true))
                    {
                        page.Render(bitmap);
                        using (var imageStream = new MemoryStream())
                        {
                            bitmap.Save(imageStream);
                            imageStream.Position = 0;
                            // We return a new MemoryStream to avoid issues with the stream being closed.
                            return ImageSource.FromStream(() => new MemoryStream(imageStream.ToArray()));
                        }
                    }
                }
            }
        }
    }
}