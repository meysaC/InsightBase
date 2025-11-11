using System.ComponentModel.DataAnnotations;

namespace InsightBase.Domain.Entities
{
    public class Document //Ana döküman, birden çok DocumentChunk içerebilir. (1:N)
    {
        public Guid Id { get; set; } = Guid.NewGuid(); //EF eklenmeden önce Id’ler belli olur böylece
        public string? UserFileName { get; set; } = string.Empty;  // users custom name
        public string FileName { get; set; } = string.Empty;  // document original name
        [Required]
        public string FilePath { get; set; } = string.Empty; // minio storage 
        public string FileType{ get; set; } = string.Empty;  // .pdf, .docx ...
        public long FileSize { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string? DocumentType { get; set; } // kanun, yonetmelik, karar
        public DateTime? PublishDate { get; set; }
        public string? LegalArea { get; set; } // medeni_hukuk, ceza_hukuku
        public bool IsPublic { get; set; } = false; // true gömülü dosya herkes erişebilir.
        public string Checksum { get; set; } = string.Empty;
        
        public string? UserId { get; set; }

        public ICollection<DocumentChunk> Chunks { get; set; } = new List<DocumentChunk>();
    }
}