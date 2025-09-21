// using Pgvector;

namespace InsightBase.Domain.Entities
{
    public class Embedding //Her DocumentChunk için oluşturulacak embedding vektörünü
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid DocumentChunkId { get; set; }
        public float[] Vector { get; set; } = Array.Empty<float>(); // 3072 boyutlu vektör (OpenAI'nin text-embedding-3-large modeli için)
        public string ModelName { get; set; } //= "text-embedding-3-large"; //Eğer ileride farklı embedding modelleriyle çalışacaksan (ör. “small” + “large”)
        public DateTime CreatedAt { get; set; }

        public DocumentChunk DocumentChunk { get; set; } = null!;
    }
}