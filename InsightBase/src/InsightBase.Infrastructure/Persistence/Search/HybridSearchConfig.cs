namespace InsightBase.Infrastructure.Persistence.Search
{
    public class HybridSearchConfig
    {
        public int MaxResults { get; set; } = 10;
        public int VectorSearchTopK { get; set; } = 20;
        public int KeywordSearchTopK { get; set; } = 20;
        public double VectorWeight { get; set; } = 0.55;
        public double BM25Weight { get; set; } = 0.35;
        public double MetadataWeight { get; set; } = 0.10;
        public bool EnableParallelSearch { get; set; } = true;    
    }
}