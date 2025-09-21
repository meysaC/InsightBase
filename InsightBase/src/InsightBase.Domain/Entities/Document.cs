using System.ComponentModel.DataAnnotations;

namespace InsightBase.Domain.Entities
{
    public class Document //Ana döküman, birden çok DocumentChunk içerebilir. (1:N)
    {
        public Guid Id { get; set; } = Guid.NewGuid(); //EF eklenmeden önce Id’ler belli olur böylece
        public string Title { get; set; } = string.Empty;

        [Required]
        public string FilePath { get; set; } = string.Empty;
        public string? ContentType { get; set; }
        public long FileSize { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? DocumentType { get; set; } // kanun, yonetmelik, karar
        public DateTime? PublishDate { get; set; }
        public string? LegalArea { get; set; } // medeni_hukuk, ceza_hukuku
        public string? Keywords { get; set; }
        public bool IsPublic { get; set; } = false; // true gömülü dosya herkes erişebilir.
        public string Checksum { get; set; } = string.Empty;
        
        public string? UserId { get; set; }

        public ICollection<DocumentChunk> Chunks { get; set; } = new List<DocumentChunk>();
    }
}