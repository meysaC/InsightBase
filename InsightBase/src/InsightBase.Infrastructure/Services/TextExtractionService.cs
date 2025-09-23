using InsightBase.Application.Interfaces;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using System.Text;
using DocumentFormat.OpenXml.Office.SpreadSheetML.Y2023.MsForms;

 using UglyToad.PdfPig;

// using iText.Kernel.Pdf;
// using iText.Kernel.Pdf.Canvas.Parser;

// using PdfSharpCore.Pdf.IO;
// using PdfSharpCore.Pdf;
// using PdfSharpCore.Pdf.Content;
// using PdfSharpCore.Pdf.Content.Objects;



namespace InsightBase.Infrastructure.Services
{
    public class TextExtractionService : ITextExtractionService
    {
        public async Task<string> ExtractTextAsync(byte[] fileContent, string fileName)
        {
            var ext = Path.GetExtension(fileName).ToLowerInvariant(); //dosya tipini göre buluyor: .pdf, .docx, .txt
            string text = ext switch
            {
                ".txt" => ExtractTextFromTxt(fileContent),//Encoding.UTF8.GetString(fileContent).Replace("\0", ""),
                ".pdf" => ExtractTextFromPdf(fileContent),
                ".docx" => ExtractTextFromDocx(fileContent),
                _ => throw new NotSupportedException($"Unsupported file type: {ext}")
            };
            return CleanExtractedText(text);
        }

        private string ExtractTextFromTxt(byte[] fileContent)
        {
            return Encoding.UTF8.GetString(fileContent).Replace("\0", "");
        }

        private string ExtractTextFromPdf(byte[] pdfBytes)
        {
            if (pdfBytes == null || pdfBytes.Length == 0)
                throw new ArgumentException("PDF içeriği boş olamaz.", nameof(pdfBytes));

            try
            {
                var sb = new StringBuilder();

                using (var stream = new MemoryStream(pdfBytes))
                using (var document = PdfDocument.Open(stream))
                {
                    int pageNumber = 1;
                    foreach (var page in document.GetPages())
                    {
                        sb.AppendLine($"--- Page {pageNumber} ---");
                        sb.AppendLine(page.Text); // tüm metni getirir
                        sb.AppendLine();

                        pageNumber++;
                    }
                }

                return sb.ToString();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("PDF metni çıkarılırken hata oluştu.", ex);
            }

            //PdfSharpCore ile text ler anlamsız karakterler geliyor
            // using var ms = new MemoryStream(pdfBytes);
            // using var pdf = PdfReader.Open(ms, PdfDocumentOpenMode.ReadOnly);
            // var sb = new StringBuilder();

            // foreach (var page in pdf.Pages)
            // {
            //     var content = ContentReader.ReadContent(page);
            //     var text = ExtractTextFromContent(content);
            //     sb.AppendLine(text);
            // }
            // return sb.ToString();

            // iText Version -> sayfaları eksik getiriyor
            // using var ms = new MemoryStream(pdfBytes);
            // using var reader = new PdfReader(ms);
            // using var pdf = new PdfDocument(reader);
            // var sb = new StringBuilder();
            // for (int i = 1; i <= pdf.GetNumberOfPages(); i++)
            // {
            //     var page = pdf.GetPage(i);
            //     sb.AppendLine(PdfTextExtractor.GetTextFromPage(page));
            // }
            // return sb.ToString();
        }

        //PDF içindeki Content objelerini recursive olarak traverse ederek metni çıkarır
        // private string ExtractTextFromContent(CObject cObject)
        // {
            //PdfSharpCore
            // if (cObject == null) return string.Empty;

            // switch (cObject)
            // {
            //     case COperator cOperator:
            //         var sbOp = new StringBuilder();
            //         foreach (var operand in cOperator.Operands)
            //         {
            //             sbOp.Append(ExtractTextFromContent(operand));
            //         }
            //         return sbOp.ToString();
            //     case CSequence cSequence:
            //         var sbSeq = new StringBuilder();
            //         foreach (var element in cSequence)
            //         {
            //             sbSeq.Append(ExtractTextFromContent(element));
            //         }
            //         return sbSeq.ToString();
            //     case CString cString:
            //         return cString.Value;
            //     default:
            //         return string.Empty;
            // }
        //}

        private string ExtractTextFromDocx(byte[] docxBytes)
        {
            // using var ms = new MemoryStream();
            // using var doc = WordprocessingDocument.Open(ms, false);

            // return string.Join("\n",
            //                         doc.MainDocumentPart.Document.Body
            //                         .Descendants<Paragraph>()
            //                         .Select(p => p.InnerText)
            //                         .Where(text => !string.IsNullOrWhiteSpace(text)));


            //PdfSharpCore
            // using var ms = new MemoryStream(docxBytes);
            // using var doc = WordprocessingDocument.Open(ms, false);

            // return string.Join("\n",
            //                         doc.MainDocumentPart.Document.Body
            //                         .Descendants<Paragraph>()
            //                         .Select(p => p.InnerText)
            //                         .Where(text => !string.IsNullOrWhiteSpace(text)));


            // İText Version
            using var ms = new MemoryStream(docxBytes);
            using var doc = WordprocessingDocument.Open(ms, false);
            return string.Join("\n", doc.MainDocumentPart.Document.Body
                                                    .Descendants<Paragraph>()
                                                    .Select(p => p.InnerText));
        }

        private string CleanExtractedText(string text)
        {
            if (string.IsNullOrEmpty(text)) return string.Empty;
            return text.Replace("\0", string.Empty);
        }
    }
}