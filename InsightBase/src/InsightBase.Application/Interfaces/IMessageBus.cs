namespace InsightBase.Application.Interfaces
{
    public interface IMessageBus
    {
        Task PublishAsync<T>(string queueNmae, T message);
        void Subscribe<T>(string queueName, Func<T, Task> handler);
    }
}