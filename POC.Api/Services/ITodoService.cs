using POC.Api.Models;

namespace POC.Api.Services
{
    public interface ITodoService
    {
        Task<IEnumerable<Todo>> GetAllTodosAsync();
        Task<Todo?> GetTodoByIdAsync(int id);
        Task<Todo> CreateTodoAsync(Todo todo);
        Task<Todo?> UpdateTodoAsync(int id, Todo todo);
        Task<bool> DeleteTodoAsync(int id);
        Task<IEnumerable<Todo>> GetTodosByCategoryAsync(string category);
        Task<IEnumerable<Todo>> GetPendingTodosAsync();
    }
}
