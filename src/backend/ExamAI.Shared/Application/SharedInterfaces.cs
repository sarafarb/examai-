namespace ExamAI.Shared.Application
{
    public interface ICurrentUserService
    {
        Guid UserId { get; }
    }
}

namespace ExamAI.Shared.Infrastructure
{
    public interface IMessagePublisher
    {
        Task PublishAsync<T>(string exchange, string routingKey, T message);
    }
}