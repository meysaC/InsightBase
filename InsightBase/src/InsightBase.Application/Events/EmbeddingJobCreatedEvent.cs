namespace InsightBase.Application.Events
{
    public record EmbeddingJobCreatedEvent(Guid DocumentId); //(parametreli ctor) DocumentId ile embedding job oluşturulduğunu belirtir
}