using Microsoft.AspNetCore.Mvc;
using POC.Api.Models;
using POC.Api.Services;

namespace POC.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MongoController : ControllerBase
    {
        private readonly IMongoService _mongoService;
        private readonly IMessageQueueService _messageQueueService;
        private readonly ILogger<MongoController> _logger;

        public MongoController(
            IMongoService mongoService,
            IMessageQueueService messageQueueService,
            ILogger<MongoController> logger)
        {
            _mongoService = mongoService;
            _messageQueueService = messageQueueService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<MongoDocument>>> GetDocuments()
        {
            try
            {
                var documents = await _mongoService.GetAllDocumentsAsync();
                return Ok(documents);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving documents");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<MongoDocument>> GetDocument(string id)
        {
            try
            {
                var document = await _mongoService.GetDocumentByIdAsync(id);
                if (document == null)
                {
                    return NotFound();
                }
                return Ok(document);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving document with ID {Id}", id);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPost]
        public async Task<ActionResult<MongoDocument>> CreateDocument(MongoDocument document)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var createdDocument = await _mongoService.CreateDocumentAsync(document);
                
                // Publish message to queue
                await _messageQueueService.PublishMessageAsync(
                    $"New document created: {createdDocument.Name}",
                    "document-created",
                    "document");

                return CreatedAtAction(nameof(GetDocument), new { id = createdDocument.Id }, createdDocument);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating document");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateDocument(string id, MongoDocument document)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var updatedDocument = await _mongoService.UpdateDocumentAsync(id, document);
                if (updatedDocument == null)
                {
                    return NotFound();
                }

                // Publish message to queue
                await _messageQueueService.PublishMessageAsync(
                    $"Document updated: {updatedDocument.Name}",
                    "document-updated",
                    "document");

                return Ok(updatedDocument);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating document with ID {Id}", id);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteDocument(string id)
        {
            try
            {
                var result = await _mongoService.DeleteDocumentAsync(id);
                if (!result)
                {
                    return NotFound();
                }

                // Publish message to queue
                await _messageQueueService.PublishMessageAsync(
                    $"Document deleted with ID: {id}",
                    "document-deleted",
                    "document");

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting document with ID {Id}", id);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet("search")]
        public async Task<ActionResult<IEnumerable<MongoDocument>>> SearchDocuments([FromQuery] string term)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(term))
                {
                    return BadRequest("Search term is required");
                }

                var documents = await _mongoService.SearchDocumentsAsync(term);
                return Ok(documents);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching documents with term {Term}", term);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet("tag/{tag}")]
        public async Task<ActionResult<IEnumerable<MongoDocument>>> GetDocumentsByTag(string tag)
        {
            try
            {
                var documents = await _mongoService.GetDocumentsByTagAsync(tag);
                return Ok(documents);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving documents by tag {Tag}", tag);
                return StatusCode(500, "Internal server error");
            }
        }
    }
}
