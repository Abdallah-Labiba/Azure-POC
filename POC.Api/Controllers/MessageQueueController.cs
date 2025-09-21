using Microsoft.AspNetCore.Mvc;
using POC.Api.Models;
using POC.Api.Services;

namespace POC.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MessageQueueController : ControllerBase
    {
        private readonly IMessageQueueService _messageQueueService;
        private readonly ILogger<MessageQueueController> _logger;

        public MessageQueueController(
            IMessageQueueService messageQueueService,
            ILogger<MessageQueueController> logger)
        {
            _messageQueueService = messageQueueService;
            _logger = logger;
        }

        [HttpPost("publish")]
        public async Task<IActionResult> PublishMessage([FromBody] MessageRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                await _messageQueueService.PublishMessageAsync(
                    request.Content,
                    request.QueueName ?? "default",
                    request.MessageType ?? "info");

                return Ok(new { message = "Message published successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error publishing message");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPost("publish/detailed")]
        public async Task<IActionResult> PublishDetailedMessage([FromBody] Message message, [FromQuery] string queueName = "default")
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                await _messageQueueService.PublishMessageAsync(message, queueName);
                return Ok(new { message = "Detailed message published successfully", messageId = message.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error publishing detailed message");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPost("queue/{queueName}")]
        public async Task<IActionResult> CreateQueue(string queueName, [FromQuery] bool durable = true)
        {
            try
            {
                await _messageQueueService.CreateQueueAsync(queueName, durable);
                return Ok(new { message = $"Queue '{queueName}' created successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating queue {QueueName}", queueName);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet("health")]
        public async Task<IActionResult> GetHealth()
        {
            try
            {
                var isHealthy = await _messageQueueService.IsHealthyAsync();
                return Ok(new { healthy = isHealthy, timestamp = DateTime.UtcNow });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking message queue health");
                return StatusCode(500, "Internal server error");
            }
        }
    }

    public class MessageRequest
    {
        public string Content { get; set; } = default!;
        public string? QueueName { get; set; }
        public string? MessageType { get; set; }
    }
}
