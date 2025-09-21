using InsightBase.Domain.Enum;

namespace InsightBase.Domain.Entities
{
    public class DocumentChunk //Dökümanın parçalara ayrılmış hali, her chunk bir embedding vektörüne sahip olabilir. (N:1 Document)
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid DocumentId { get; set; }

        public string Content { get; set; } = string.Empty;

        public int ChunkIndex { get; set; } // chunk sırası (arama/retrieval için önemli)
        public int StartToken { get; set; }
        public int EndToken { get; set; }

        public int Length { get; set; }
        public DateTime CreatedAt { get; set; }

        public ChunkStatus Status { get; set; } = ChunkStatus.Pending; // default olarak pending
        public Document? Document { get; set; } = null!;
        public Embedding? Embedding { get; set; } // 1:1 relation 
        //Eğer aynı chunk için birden fazla embedding modeli (ör. farklı boyutlu vektörler) kullanma  ICollection<Embedding> Embeddings ekle
    }
}