using MongoDB.Driver;
using POC.Api.Models;

namespace POC.Api.Services
{
    public class MongoService : IMongoService
    {
        private readonly IMongoCollection<MongoDocument> _collection;
        private readonly ILogger<MongoService> _logger;

        public MongoService(IMongoClient mongoClient, ILogger<MongoService> logger)
        {
            _logger = logger;
            var database = mongoClient.GetDatabase("PocDb");
            _collection = database.GetCollection<MongoDocument>("documents");
        }

        public async Task<IEnumerable<MongoDocument>> GetAllDocumentsAsync()
        {
            try
            {
                var documents = await _collection
                    .Find(_ => true)
                    .SortByDescending(d => d.CreatedAt)
                    .ToListAsync();
                
                return documents;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all documents");
                throw;
            }
        }

        public async Task<MongoDocument?> GetDocumentByIdAsync(string id)
        {
            try
            {
                var document = await _collection
                    .Find(d => d.Id == id)
                    .FirstOrDefaultAsync();
                
                return document;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving document with ID {Id}", id);
                throw;
            }
        }

        public async Task<MongoDocument> CreateDocumentAsync(MongoDocument document)
        {
            try
            {
                document.CreatedAt = DateTime.UtcNow;
                await _collection.InsertOneAsync(document);
                
                _logger.LogInformation("Created document with ID {Id}", document.Id);
                return document;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating document");
                throw;
            }
        }

        public async Task<MongoDocument?> UpdateDocumentAsync(string id, MongoDocument document)
        {
            try
            {
                document.Id = id;
                document.UpdatedAt = DateTime.UtcNow;
                
                var result = await _collection.ReplaceOneAsync(
                    d => d.Id == id,
                    document,
                    new ReplaceOptions { IsUpsert = false });
                
                if (result.MatchedCount == 0)
                {
                    return null;
                }
                
                _logger.LogInformation("Updated document with ID {Id}", id);
                return document;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating document with ID {Id}", id);
                throw;
            }
        }

        public async Task<bool> DeleteDocumentAsync(string id)
        {
            try
            {
                var result = await _collection.DeleteOneAsync(d => d.Id == id);
                
                _logger.LogInformation("Deleted document with ID {Id}, count: {Count}", id, result.DeletedCount);
                return result.DeletedCount > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting document with ID {Id}", id);
                throw;
            }
        }

        public async Task<IEnumerable<MongoDocument>> SearchDocumentsAsync(string searchTerm)
        {
            try
            {
                var filter = Builders<MongoDocument>.Filter.Or(
                    Builders<MongoDocument>.Filter.Regex(d => d.Name, new MongoDB.Bson.BsonRegularExpression(searchTerm, "i")),
                    Builders<MongoDocument>.Filter.Regex(d => d.Content, new MongoDB.Bson.BsonRegularExpression(searchTerm, "i"))
                );

                var documents = await _collection
                    .Find(filter)
                    .SortByDescending(d => d.CreatedAt)
                    .ToListAsync();
                
                return documents;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching documents with term {SearchTerm}", searchTerm);
                throw;
            }
        }

        public async Task<IEnumerable<MongoDocument>> GetDocumentsByTagAsync(string tag)
        {
            try
            {
                var documents = await _collection
                    .Find(d => d.Tags.Contains(tag))
                    .SortByDescending(d => d.CreatedAt)
                    .ToListAsync();
                
                return documents;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving documents by tag {Tag}", tag);
                throw;
            }
        }
    }
}
