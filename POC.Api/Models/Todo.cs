using System.ComponentModel.DataAnnotations;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace POC.Api.Models
{
    public class Todo
    {
        public int Id { get; set; }
        
        [Required]
        [StringLength(200)]
        public string Title { get; set; } = default!;
        
        public bool Done { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime? UpdatedAt { get; set; }
        
        [StringLength(500)]
        public string? Description { get; set; }
        
        public string? Category { get; set; }
        
        public int Priority { get; set; } = 1; // 1-5 scale
    }

    public class MongoDocument
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }
        
        public string Name { get; set; } = default!;
        
        public string Content { get; set; } = default!;
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime? UpdatedAt { get; set; }
        
        public Dictionary<string, object> Metadata { get; set; } = new();
        
        public List<string> Tags { get; set; } = new();
    }

    public class Message
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        
        public string Content { get; set; } = default!;
        
        public string Type { get; set; } = "info";
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public string? Source { get; set; }
        
        public Dictionary<string, object> Properties { get; set; } = new();
    }
}
