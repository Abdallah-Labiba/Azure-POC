using POC.Api.Models;

namespace POC.Api.Services
{
    public interface IMongoService
    {
        Task<IEnumerable<MongoDocument>> GetAllDocumentsAsync();
        Task<MongoDocument?> GetDocumentByIdAsync(string id);
        Task<MongoDocument> CreateDocumentAsync(MongoDocument document);
        Task<MongoDocument?> UpdateDocumentAsync(string id, MongoDocument document);
        Task<bool> DeleteDocumentAsync(string id);
        Task<IEnumerable<MongoDocument>> SearchDocumentsAsync(string searchTerm);
        Task<IEnumerable<MongoDocument>> GetDocumentsByTagAsync(string tag);
    }
}
