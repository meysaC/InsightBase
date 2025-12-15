namespace InsightBase.Infrastructure.Persistence.Search
{
    public class FilterStatistics
    {
        public int TotalResults { get; set; }
        public Dictionary<string, int> LegalAreaDistribution { get; set; } = new();
        public Dictionary<string, int> CourtDistribution { get; set; } = new();
        public Dictionary<string, int> DocumentTypeDistribution { get; set; } = new();
        public Dictionary<string, int> DateRangeDistribution { get; set; } = new();
        public int AmendedCount { get; set; }
        public int CurrentCount { get; set; }
    }
}