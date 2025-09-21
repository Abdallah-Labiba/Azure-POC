using Azure.Messaging.ServiceBus;
using System.Text;
using System.Text.Json;
using POC.Api.Models;

namespace POC.Api.Services
{
    public class MessageQueueService : IMessageQueueService, IDisposable
    {
        private readonly ServiceBusClient _serviceBusClient;
        private readonly ILogger<MessageQueueService> _logger;
        private readonly Dictionary<string, ServiceBusSender> _senders = new();
        private bool _disposed = false;

        public MessageQueueService(ServiceBusClient serviceBusClient, ILogger<MessageQueueService> logger)
        {
            _serviceBusClient = serviceBusClient;
            _logger = logger;
        }

        public async Task PublishMessageAsync(Message message, string queueName = "default")
        {
            try
            {
                var sender = GetOrCreateSender(queueName);
                
                var json = JsonSerializer.Serialize(message);
                var serviceBusMessage = new ServiceBusMessage(json)
                {
                    MessageId = message.Id.ToString(),
                    ContentType = "application/json",
                    Subject = message.Type,
                    TimeToLive = TimeSpan.FromDays(7) // Set message TTL
                };

                // Add custom properties
                foreach (var property in message.Properties)
                {
                    serviceBusMessage.ApplicationProperties[property.Key] = property.Value;
                }

                await sender.SendMessageAsync(serviceBusMessage);
                
                _logger.LogInformation("Published message {MessageId} to queue {QueueName}", message.Id, queueName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error publishing message to queue {QueueName}", queueName);
                throw;
            }
        }

        public async Task PublishMessageAsync(string content, string queueName = "default", string messageType = "info")
        {
            var message = new Message
            {
                Content = content,
                Type = messageType,
                Source = "POC.Api"
            };
            
            await PublishMessageAsync(message, queueName);
        }

        public async Task StartConsumingAsync(string queueName, Func<Message, Task> messageHandler)
        {
            try
            {
                var processor = _serviceBusClient.CreateProcessor(queueName, new ServiceBusProcessorOptions
                {
                    MaxConcurrentCalls = 1,
                    AutoCompleteMessages = false
                });

                processor.ProcessMessageAsync += async args =>
                {
                    try
                    {
                        var json = args.Message.Body.ToString();
                        var message = JsonSerializer.Deserialize<Message>(json);
                        
                        if (message != null)
                        {
                            await messageHandler(message);
                            await args.CompleteMessageAsync(args.Message);
                            _logger.LogInformation("Processed message {MessageId} from queue {QueueName}", message.Id, queueName);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing message from queue {QueueName}", queueName);
                        await args.AbandonMessageAsync(args.Message);
                    }
                };

                processor.ProcessErrorAsync += args =>
                {
                    _logger.LogError(args.Exception, "Error processing message from queue {QueueName}", queueName);
                    return Task.CompletedTask;
                };

                await processor.StartProcessingAsync();
                _logger.LogInformation("Started consuming from queue {QueueName}", queueName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting consumer for queue {QueueName}", queueName);
                throw;
            }
        }

        public Task<bool> IsHealthyAsync()
        {
            try
            {
                return Task.FromResult(!_serviceBusClient.IsClosed);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking Service Bus health");
                return Task.FromResult(false);
            }
        }

        public Task CreateQueueAsync(string queueName, bool durable = true)
        {
            try
            {
                // Note: In Azure Service Bus, queues are typically created through Azure portal or ARM templates
                // This method is kept for compatibility but doesn't actually create queues
                _logger.LogDebug("Queue {QueueName} verification requested (queues are pre-created in Azure Service Bus)", queueName);
                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying queue {QueueName}", queueName);
                throw;
            }
        }

        private ServiceBusSender GetOrCreateSender(string queueName)
        {
            if (!_senders.ContainsKey(queueName))
            {
                var sender = _serviceBusClient.CreateSender(queueName);
                _senders[queueName] = sender;
            }
            
            return _senders[queueName];
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                foreach (var sender in _senders.Values)
                {
                    try
                    {
                        sender.CloseAsync().GetAwaiter().GetResult();
                        sender.DisposeAsync().GetAwaiter().GetResult();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Error disposing sender");
                    }
                }
                _senders.Clear();
                
                try
                {
                    _serviceBusClient.DisposeAsync().GetAwaiter().GetResult();
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error disposing Service Bus client");
                }
                
                _disposed = true;
            }
        }
    }
}

