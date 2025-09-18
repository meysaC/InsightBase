namespace InsightBase.Application.Interfaces
{
    public interface ITextExtractionService
    {
        Task<string> ExtractTextAsync(byte[] fileContent, string fileName); //PDF, DOCX, TXT parser’lıcak
    }
}