using InsightBase.Application.Models;
using MediatR;

namespace InsightBase.Application.Commands.Chat
{
    // Record’lar default olarak immutable kullanıma çok uygun 
    // immutable veri akışı özellikle CQRS’de tavsiye edilir.
    // Command ve Query nesnelerinin “taşıyıcı” olduğu düşünülür, set edilmez, değişmez.
    public record AnalyzeQueryCommand(string Query, string? UserId = null) : IRequest<QueryContext> ;
}