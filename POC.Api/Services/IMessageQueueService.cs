using POC.Api.Models;

namespace POC.Api.Services
{
    public interface IMessageQueueService
    {
        Task PublishMessageAsync(Message message, string queueName = "default");
        Task PublishMessageAsync(string content, string queueName = "default", string messageType = "info");
        Task StartConsumingAsync(string queueName, Func<Message, Task> messageHandler);
        Task<bool> IsHealthyAsync();
        Task CreateQueueAsync(string queueName, bool durable = true);
    }
}
