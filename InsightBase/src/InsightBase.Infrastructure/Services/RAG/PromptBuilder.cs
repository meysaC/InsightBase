using System.Text;
using InsightBase.Application.Interfaces;
using InsightBase.Application.Models;
using InsightBase.Application.Models.Enum;
using Microsoft.Extensions.Configuration;

namespace InsightBase.Infrastructure.Services.RAG
{
    public class PromptBuilder : IPromptBuilder
    {
        private readonly IConfiguration _config;
        public PromptBuilder(IConfiguration config) => _config = config;
        public string BuildPrompt(QueryContext queryContext, List<SearchResult> searchResults, string userQuery)
        {
            var sb = new StringBuilder();

            // 1. sistem yönlendirmeleri
            sb.AppendLine(BuildSystemInstruction(queryContext));
            sb.AppendLine();

            // 2. Hukuki kurallar ve kısıtlamalar
            sb.AppendLine(BuildLegalGuidelines());
            sb.AppendLine();

            // 3. kaynak dökümanlar
            sb.AppendLine("# KAYNAK DÖKÜMANLAR");
            sb.AppendLine();

            int docIndex = 0;
            foreach (var result in searchResults)
            {
                sb.AppendLine(FormatDocument(result, docIndex));
                sb.AppendLine();
                docIndex++;
            }

            // 4. query context (metadata)
            sb.AppendLine("# SORGU BİLGİLERİ");
            sb.AppendLine(FormatQueryContext(queryContext));
            sb.AppendLine();

            // 5. kullanıcı sorgusu
            sb.AppendLine("# KULLANICI SORUSU");
            sb.AppendLine(userQuery);
            sb.AppendLine();

            // 6. output format
            sb.AppendLine(BuildOutputFormat(queryContext));

            return sb.ToString();
        }


        private string BuildSystemInstruction(QueryContext queryContext)
        {
            var instruction = new StringBuilder();

            instruction.AppendLine("Sen bir Türk hukuku uzmanı asistanısın.");
            instruction.AppendLine();
            instruction.AppendLine("**TEMEL KURALLAR:**");
            instruction.AppendLine("1. SADECE verilen kaynak dokümanlardan hareketle cevap üret");
            instruction.AppendLine("2. Kendi bilgini kullanma veya tahmin yapma");
            instruction.AppendLine("3. Verilmeyen bilgiyi uydurmadan açıkça söyle: \"Bu bilgi kaynaklarda mevcut değil\"");
            instruction.AppendLine("4. Her ifade için mutlaka kaynak referansı ver: [KAYNAK-X]");
            instruction.AppendLine("5. Kanun maddesi geçiyorsa, güncellik kontrolü yap");
            instruction.AppendLine("6. Tarih bilgisi veriyorsan doğruluğunu kontrol et");
            instruction.AppendLine("7. Belirsizlik varsa açıkça belirt");

            // Intent e göre özel talimatlar
            if (queryContext.Intents.Contains("case_search"))
            {
                instruction.AppendLine();
                instruction.AppendLine("**İÇTİHAT ARAMA TALİMATLARI:**");
                instruction.AppendLine("- İçtihat kararının tarihi, dairesi ve dosya numarasını belirt");
                instruction.AppendLine("- Kararın ana hukuki ilkesini özetle");
                instruction.AppendLine("- Benzer durumları grupla");
            }

            if (queryContext.Intents.Contains("article_explanation"))
            {
                instruction.AppendLine();
                instruction.AppendLine("**MADDE AÇIKLAMA TALİMATLARI:**");
                instruction.AppendLine("- Madde metnini aynen aktar");
                instruction.AppendLine("- Maddenin amacını açıkla");
                instruction.AppendLine("- Güncel mi, değişmiş mi kontrol et");
                instruction.AppendLine("- İlgili içtihatlarla destekle");
            }

            if (queryContext.RequiresCaseLaw && queryContext.RequiresLegislation)
            {
                instruction.AppendLine();
                instruction.AppendLine("**KARMA YANIT TALİMATI:**");
                instruction.AppendLine("- Önce kanuni düzenlemeyi açıkla");
                instruction.AppendLine("- Sonra içtihat uygulamasını göster");
                instruction.AppendLine("- İkisi arasındaki ilişkiyi kur");
            }

            return instruction.ToString();
        }
        private string BuildLegalGuidelines()
        {
            return @"
                **HUKUKİ HASSAS NOKTALAR:**

                1. **Kaynak Güvenilirliği:**
                - Kanun metni > Yargıtay kararı > Akademik yorum sıralamasını takip et
                - Çelişen kaynaklar varsa hepsini belirt ve uyarı ver

                2. **Tarih ve Güncellik:**
                - Kanun maddesi belirtiyorsan ""Bu madde X tarihinde yürürlüğe girmiştir"" ekle
                - Eski tarihli içtihatlar için ""O tarihte geçerli olan düzenlemeye göre..."" ifadesi kullan
                - Değişiklik/yürürlükten kaldırma durumunu mutlaka belirt

                3. **Madde Atıfları:**
                - Tam referans ver: ""Türk Ceza Kanunu'nun 86. maddesi""
                - Fıkra/bent varsa belirt: ""TCK md. 86/1-a""
                - Yanlış madde numarası verme, emin değilsen atıf yapma

                4. **Hukuki Sorumluluk:**
                - ""Bu bir hukuki görüştür, kesin hukuki danışma değildir"" uyarısı ekle
                - Somut vaka için ""Bir avukata danışılması önerilir"" ifadesi kullan

                5. **Belirsizlik Yönetimi:**
                - Kaynaklarda çelişki varsa: ""Kaynaklarda farklı görüşler mevcut""
                - Güncellik şüphesi varsa: ""Bu bilgi güncelleme gerektirebilir""
                - Eksik bilgi varsa: ""Daha detaylı inceleme gereklidir""
                ";        
        }
        private string FormatDocument(SearchResult result, int docIndex)
        {
            var sb = new StringBuilder();

            // kaynak id
            sb.AppendLine($"## [KAYNAK-{docIndex}]");
            sb.AppendLine();

            // metadata
            sb.AppendLine("**Kaynak Bilgileri:**");
            sb.AppendLine($"- Tür: {GetDocumentTypeDescription(result.DocumentType)}");
            sb.AppendLine($"- Başlık: {result.Title}");
            
            if (!string.IsNullOrEmpty(result.Court))
                sb.AppendLine($"- Mahkeme/Daire: {result.Court}");
            
            if (result.PublishDate.HasValue)
                sb.AppendLine($"- Tarih: {result.PublishDate:dd.MM.yyyy}");
            
            if (result.LawReferences.Any())
                sb.AppendLine($"- İlgili Kanun: {string.Join(", ", result.LawReferences)}");
            
            if (!string.IsNullOrEmpty(result.FileNumber))
                sb.AppendLine($"- Dosya No: {result.FileNumber}");

            sb.AppendLine();

            // content
            sb.AppendLine("**İçerik:**");
            sb.AppendLine(result.Content);

            // güncellik uyarısı
            if (result.PublishDate.HasValue)
            {
                var age = DateTime.UtcNow - result.PublishDate.Value;
                if (age.TotalDays > 365 * 5) // 5 yıldan eski
                {
                    sb.AppendLine();
                    sb.AppendLine($"⚠️ **UYARI:** Bu kaynak {result.PublishDate:yyyy} tarihlidir. Güncel mevzuat kontrol edilmelidir.");
                }
            }

            // değişiklik bilgisi varsa
            if (result.IsAmended)
            {
                sb.AppendLine();
                sb.AppendLine($"⚠️ **UYARI:** Bu düzenleme {result.AmendmentDate:dd.MM.yyyy} tarihinde değiştirilmiştir.");
            }

            return sb.ToString();        
        }
        private string FormatQueryContext(QueryContext queryContext)
        {
            var sb = new StringBuilder();

            if (queryContext.LegalAreas.Any())
                sb.AppendLine($"- Hukuk Alanı: {string.Join(", ", queryContext.LegalAreas)}");

            if (queryContext.LawReferences.Any())
                sb.AppendLine($"- İlgili Kanunlar: {string.Join(", ", queryContext.LawReferences)}");

            if (queryContext.Courts.Any())
                sb.AppendLine($"- Mahkeme/Daire: {string.Join(", ", queryContext.Courts)}");

            if (queryContext.StartDate.HasValue)
                sb.AppendLine($"- Tarih Aralığı: {queryContext.StartDate:dd.MM.yyyy} - {queryContext.EndDate:dd.MM.yyyy}");

            if (queryContext.Intents.Any())
                sb.AppendLine($"- Arama Amacı: {GetIntentDescription(queryContext.Intents)}");

            return sb.ToString();
        }
        private string BuildOutputFormat(QueryContext queryContext)
        {
            var sb = new StringBuilder();

            sb.AppendLine("# CEVAP FORMATI");
            sb.AppendLine();
            sb.AppendLine("Cevabını aşağıdaki yapıda ver:");
            sb.AppendLine();

            if (queryContext.Intents.Contains("case_search"))
            {
                sb.AppendLine(@"
                    **1. ÖZET**
                    [Kısa özet, 2-3 cümle]

                    **2. İLGİLİ KARARLAR**
                    - [Karar 1] [KAYNAK-X]
                    - Daire: ...
                    - Tarih: ...
                    - Sonuç: ...
                    
                    **3. HUKUKİ İLKE**
                    [Ana hukuki ilke] [KAYNAK-X]

                    **4. SONUÇ**
                    [Genel değerlendirme]

                    **KAYNAKLAR:**
                    [Kullanılan tüm kaynakları listele]
                    ");
            }
            else if (queryContext.Intents.Contains("article_explanation"))
            {
                sb.AppendLine(@"
                    **1. MADDE METNİ**
                    [Madde metnini aynen aktar] [KAYNAK-X]

                    **2. AÇIKLAMA**
                    [Maddenin amacı ve kapsamı] [KAYNAK-X]

                    **3. UYGULAMA**
                    [İçtihat uygulaması varsa] [KAYNAK-X]

                    **4. GÜNCELLIK**
                    [Madde güncel mi, değişiklik var mı?]

                    **KAYNAKLAR:**
                    [Kullanılan tüm kaynakları listele]
                    ");
            }
            else
            {
                sb.AppendLine(@"
                    **1. KISA CEVAP**
                    [2-3 cümlelik özet]

                    **2. DETAYLI AÇIKLAMA**
                    [Kaynaklara dayalı açıklama] [KAYNAK-X]

                    **3. SONUÇ VE TAVSİYELER**
                    [Özet ve öneriler]

                    **KAYNAKLAR:**
                    [Kullanılan tüm kaynakları listele]

                    **YASAL UYARI:**
                    Bu bilgi genel hukuki bir görüştür. Somut durumunuz için mutlaka bir avukata danışınız.
                    ");
            }
            
            return sb.ToString();
        }


        private string GetDocumentTypeDescription(DocumentType documentType)
        {
            return documentType switch
            {
                DocumentType.Legislation => "Kanun/Mevzuat",
                DocumentType.CaseLaw => "İçtihat/Yargı Kararı",
                DocumentType.Commentary => "Akademik Yorum/Makale",
                DocumentType.Regulation => "Yönetmelik/Tüzük",
                _ => "Diğer"
            };        
        }
        private string GetIntentDescription(List<string> intents)
        {
            return string.Join(", ", intents.Select(i => i switch
            {
                "case_search" => "İçtihat Arama",
                "article_explanation" => "Madde Açıklaması",
                "comparison" => "Karşılaştırma",
                "precedent_search" => "Emsal Arama",
                "general_legal_question" => "Genel Hukuki Soru",
                _ => i
            }));
        }

    }
}