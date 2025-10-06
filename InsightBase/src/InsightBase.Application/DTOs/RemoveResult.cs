namespace InsightBase.Application.DTOs
{
    public class RemoveResult
    {
        public List<string> Successful { get; set; } = new();
        public List<string> Failed { get; set; } = new();
    }
}