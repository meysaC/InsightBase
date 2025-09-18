using InsightBase.Application.Interfaces;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using System.IO;
using System.Text;


namespace InsightBase.Infrastructure.Services
{
    public class TextExtractionService : ITextExtractionService
    {
        public async Task<string> ExtractTextAsync(byte[] fileContent, string fileName)
        {
            var ext = Path.GetExtension(fileName).ToLowerInvariant(); //dosya tipini göre buluyor: .pdf, .docx, .txt
            return ext switch
            {
                ".txt" => Encoding.UTF8.GetString(fileContent).Replace("\0", ""),
                ".pdf" => ExtractTextFromPdf(fileContent),
                ".docx" => ExtractTextFromDocx(fileContent),
                _ => throw new NotSupportedException($"Unsupported file type: {ext}")
            };
        }
        private string ExtractTextFromPdf(byte[] pdfBytes)
        {
            using var ms = new MemoryStream(pdfBytes);
            using var reader = new PdfReader(ms);
            using var pdf = new PdfDocument(reader);
            var sb = new StringBuilder();
            for (int i = 1; i <= pdf.GetNumberOfPages(); i++)
            {
                var page = pdf.GetPage(i);
                sb.AppendLine(PdfTextExtractor.GetTextFromPage(page));
            }
            return sb.ToString();
        }
        private string ExtractTextFromDocx(byte[] docxBytes)
        {
            using var ms = new MemoryStream(docxBytes);
            using var doc = WordprocessingDocument.Open(ms, false);
            return string.Join("\n", doc.MainDocumentPart.Document.Body
                                                    .Descendants<Paragraph>()
                                                    .Select(p => p.InnerText));
        }
    }
}