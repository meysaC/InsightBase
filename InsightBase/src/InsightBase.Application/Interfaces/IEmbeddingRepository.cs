namespace InsightBase.Application.Interfaces
{
    public interface IEmbeddingRepository
    {
        Task AddEmbeddingEntitiesAsync(Domain.Entities.Embedding embeddings);
        Task<Domain.Entities.Embedding?> GetEmbeddingEntitiesByChunkIdAsync(Guid chunkId);
    }
}