using DocumentFormat.OpenXml.Bibliography;
using DocumentFormat.OpenXml.Drawing;
using InsightBase.Application.Models;
using System.Text.RegularExpressions;

namespace InsightBase.Infrastructure.Services.QueryAnalyzer
{
    public partial class RegexExtractor  // Deterministik regex tabanlı bilgi çıkarımı yapan servis. 
                                // Kanun referansları, tarih aralıkları, mahkeme bilgileri vb. çıkarır.
    {
        [GeneratedRegex(@"(?:(TCK|TBK|CMK|HMK|TMK|İİK|TTK|VUK|SGK|İş\s+Kanunu|Anayasa)\s*(?:md\.?|madde)?\s*(\d+)(?:[\/\-](\d+))?(?:[\/\-]([a-zçğıöşü]+))?)", 
            RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.CultureInvariant)]
        private static partial Regex LawReferencePattern();


        [GeneratedRegex(@"son\s+(\d+)\s+(yıl|ay|gün)", 
            RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.CultureInvariant)]
        private static partial Regex RelativeDatePattern();


        [GeneratedRegex(@"(\d{4})[\/\-\.](\d{1,2})[\/\-\.](\d{1,2})", 
            RegexOptions.Compiled)]
        private static partial Regex AbsoluteDatePattern();


        [GeneratedRegex(@"(Yargıtay|Danıştay|AYM|Bölge\s+Adliye\s+Mahkemesi)\s+(\d+)\.?\s*(Ceza|Hukuk|İdari|Vergi)?\s*(Daire|Dairesi)?", 
            RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.CultureInvariant)]
        private static partial Regex CourtPattern();


        [GeneratedRegex(@"E\.\s*(\d{4})/(\d+)", 
            RegexOptions.Compiled)]
        private static partial Regex FileNumberPattern();


        [GeneratedRegex(@"(ceza|medeni|ticaret|borçlar|iş|idare|anayasa)\s+(hukuku?)?", 
            RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.CultureInvariant)]
        private static partial Regex LegalAreaPattern();


        public RegexExtractionResult Extract(string query) // sorgudan tüm bilgileri çıkarır
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return new RegexExtractionResult();
            }

            var result = new RegexExtractionResult
            {
                OriginalQuery = query,
            };
            
            // Kanun referanslarını çıkar
            result.LawReferences = ExtractLawReferences(query);
            
            // Tarih aralıklarını çıkar
            ExtractDateRange(query, result);
            
            // Mahkeme bilgilerini çıkar
            result.Courts = ExtractCourts(query);
           
            // Dosya numaralarını çıkar
            result.FileNumbers = ExtractFileNumbers(query);
            
            // Hukuk alanlarını çıkar
            result.LegalAreas = ExtractLegalAreas(query);
            
            return result;
        }


        private List<string> ExtractLawReferences(string query) //Kanun referanslarını çıkarır (TCK 86, TBK 49/1-a gibi)
        {
            var lafReferances = LawReferencePattern()
                                .Matches(query)
                                .Select(m => NormalizeLawReference(m))
                                .Distinct(StringComparer.OrdinalIgnoreCase)
                                .ToList();  
            return lafReferances;
        }
        private static string NormalizeLawReference(Match match) // Kanun referanslarını standart formata dönüştürür
        {
            var law = match.Groups[1].Value.ToUpperInvariant();
            var article = match.Groups[2].Value;
            var subArticle = match.Groups[3].Success ? $"/{match.Groups[3].Value}" : "";
            var subParagraph = match.Groups[4].Success ? $"-{match.Groups[4].Value}" : "";

            return $"{law} {article}{subArticle}{subParagraph}";
        }


        private void ExtractDateRange(string query, RegexExtractionResult result) // Tarih aralıklarını çıkarır
        {
            // göreceli tarih (son 5 yıl, son 3ay)
            var relativeMatch = RelativeDatePattern().Match(query);
            if(relativeMatch.Success)
            {
                int value = int.Parse(relativeMatch.Groups[1].Value);
                string unit = relativeMatch.Groups[2].Value.ToLowerInvariant();

                result.EndDate = DateTime.UtcNow;
                result.StartDate = unit switch
                {
                    "yıl" => DateTime.UtcNow.AddYears(-value),
                    "ay" => DateTime.UtcNow.AddMonths(-value),
                    "gün" => DateTime.UtcNow.AddDays(-value),
                    _ => result.StartDate
                };
                return;
            }

            // mutlak tarih ( 2021-12-01 formatında)
            var absoluteMatches = AbsoluteDatePattern().Matches(query);
            if(absoluteMatches.Count > 0)
            {
                var dates = absoluteMatches
                            .Select(m => ParseDate(m))
                            .Where(d => d.HasValue)
                            .Select(d => d!.Value)
                            .ToList();
                if(dates.Any())
                {
                    result.StartDate = dates.First();
                    result.EndDate = dates.Count > 1 ? dates.Last() : DateTime.UtcNow;
                }
            }
        }
        private static DateTime? ParseDate(Match match) // Tarihleri DateTime formatına dönüştürür
        {
            try
            {
                int year = int.Parse(match.Groups[1].Value);
                int month = int.Parse(match.Groups[2].Value);
                int day = int.Parse(match.Groups[3].Value);

                return new DateTime(year, month, day);
            }
            catch
            {
                return null;
            }
        }
    
    
        private List<string> ExtractCourts(string query) // Mahkeme bilgilerini çıkarır
        {
            var courts = CourtPattern()
                        .Matches(query)
                        .Select(m => NormalizeCourt(m))
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .ToList();
            return courts;
        }
        private static string NormalizeCourt(Match match) // mahkeme bilgilerini standart formata dönüştürür
        {
            var courtName = match.Groups[1].Value;
            var courtNumber = match.Groups[2].Value;
            var courtType = match.Groups[3].Success ? match.Groups[3].Value : "";
            var courtDaire = match.Groups[4].Success ? "Dairesi" : "";

            return $"{courtName} {courtNumber}. {courtType} {courtDaire}".Trim();
        }
    

        private List<string> ExtractFileNumbers(string query) // Dosya numaralarını çıkarır (E. 2021/1234 gibi)
        {
            var fileNumbers = FileNumberPattern()
                              .Matches(query)
                              .Select(m => $"E.{m.Groups[1].Value}/{m.Groups[2].Value}")
                              .Distinct(StringComparer.OrdinalIgnoreCase)
                              .ToList();
            return fileNumbers;
        }


        private List<string> ExtractLegalAreas(string query) // Hukuk alanlarını çıkarır (ceza hukuku, medeni hukuk gibi)
        {
            var legalAreas = LegalAreaPattern()
                             .Matches(query)
                             .Select(m => NormalizeLegalArea(m.Groups[1].Value))
                             .Distinct(StringComparer.OrdinalIgnoreCase)
                             .ToList();
            return legalAreas;
        }
        private static string NormalizeLegalArea(string area) // Hukuk alanlarını standart formata dönüştürür
        {
            return area.ToLowerInvariant() switch
            {
                "ceza" => "ceza_hukuku",
                "medeni" => "medeni_hukuku",
                "ticaret" => "ticaret_hukuku",
                "borçlar" => "borçlar_hukuku",
                "iş" => "iş_hukuku",
                "idare" => "idare_hukuku",
                "anayasa" => "anayasa_hukuku",
                _ => $"{area}_hukuku"
            };
        }
    
    }
}