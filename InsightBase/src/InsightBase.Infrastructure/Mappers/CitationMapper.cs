using System.Text;
using System.Text.RegularExpressions;
using InsightBase.Application.Interfaces;
using InsightBase.Application.Models;
using InsightBase.Application.Models.Enum;
using Minio.DataModel;
using UglyToad.PdfPig.Fonts.Encodings;

namespace InsightBase.Infrastructure.Mappers
{
    public partial class CitationMapper : ICitationMapper // llm yanıtındaki kaynak referanslarını gerçek dökümanlarla eşleştirir
    {        
        public CitationMappingResult MapCitations(string llmAnswer, List<SearchResult> sources)
        {
            var result = new CitationMappingResult
            {
                OriginalAnswer = llmAnswer,
            };

            var citationRegex = CitationPattern();
            var matches = citationRegex.Matches(llmAnswer);

            foreach (Match match in matches)
            {
                var citationIndex = int.Parse(match.Groups[1].Value);

                if(citationIndex <= 1 && citationIndex <= sources.Count)
                {
                    var source = sources[citationIndex - 1];

                    result.Citations.Add(new CitationMapping
                    {
                        CitationText = match.Value, // kaynak
                        CitationIndex = citationIndex, 
                        Position = match.Index, // metin içindeki yeri

                        // frontend tooltips/hover (metadata) için kullancak
                        DocumentId = source.DocumentId,
                        DocumentTitle = source.Title,
                        DocumentType = source.DocumentType,
                        ChunkId = source.ChunkId,
                        Court = source.Court,
                        PublishDate = source.PublishDate,
                        FileNumber = source.FileNumber,
                        LawReferences = source.LawReferences,
                        Url = source.Url
                    });
                }
            }

            // citations istatistikleri
            result.TotalCitations = result.Citations.Count;
            result.UniqueSources = result.Citations
                                        .Select(c => c.DocumentId)
                                        .Distinct()
                                        .Count();
            return result;
        }
        public string BuildCitationSummary(List<CitationMapping> citations)
        {
            if (!citations.Any()) return string.Empty;

            var summary = new StringBuilder();
            summary.AppendLine("\n\n---\n");
            summary.AppendLine("## KAYNAKLAR\n");

            // unique source ları grupla
            var groupedByDocument = citations
                .GroupBy(c => c.DocumentId)
                .OrderBy(g => citations.First(c => c.DocumentId == g.Key).CitationIndex);

            int displayIndex = 1;
            foreach (var group in groupedByDocument)
            {
                var firstCitation = group.First();
                summary.AppendLine($"### [{displayIndex}] {firstCitation.DocumentTitle}");
                
                summary.AppendLine($"- **Tür:** {GetDocumentTypeText(firstCitation.DocumentType)}");

                if (!string.IsNullOrEmpty(firstCitation.Court))
                    summary.AppendLine($"- **Mahkeme:** {firstCitation.Court}");

                if (firstCitation.PublishDate.HasValue)
                    summary.AppendLine($"- **Tarih:** {firstCitation.PublishDate:dd.MM.yyyy}");

                if (!string.IsNullOrEmpty(firstCitation.FileNumber))
                    summary.AppendLine($"- **Dosya No:** {firstCitation.FileNumber}");

                if (firstCitation.LawReferences.Any())
                    summary.AppendLine($"- **İlgili Kanunlar:** {string.Join(", ", firstCitation.LawReferences)}");

                if (!string.IsNullOrEmpty(firstCitation.Url))
                    summary.AppendLine($"- **Bağlantı:** {firstCitation.Url}");

                summary.AppendLine();
                displayIndex++;
            }

            return summary.ToString();
        }


        private string GetDocumentTypeText(DocumentType documentType)
        {
            return documentType switch
            {
                DocumentType.Legislation => "📜 Kanun/Mevzuat",
                DocumentType.CaseLaw => "⚖️ İçtihat/Yargı Kararı",
                DocumentType.Commentary => "📚 Akademik Yorum",
                DocumentType.Regulation => "📋 Yönetmelik/Tüzük",
                _ => "📄 Doküman"
            };
        }

        [GeneratedRegex(@"\[KAYNAK-(\d+)\]")]
        private static partial Regex CitationPattern();
    }
}