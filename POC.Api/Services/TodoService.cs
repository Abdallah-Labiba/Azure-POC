using Microsoft.EntityFrameworkCore;
using POC.Api.Data;
using POC.Api.Models;

namespace POC.Api.Services
{
    public class TodoService : ITodoService
    {
        private readonly AppDb _context;
        private readonly ILogger<TodoService> _logger;

        public TodoService(AppDb context, ILogger<TodoService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IEnumerable<Todo>> GetAllTodosAsync()
        {
            try
            {
                return await _context.Todos
                    .AsNoTracking()
                    .OrderByDescending(t => t.CreatedAt)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all todos");
                throw;
            }
        }

        public async Task<Todo?> GetTodoByIdAsync(int id)
        {
            try
            {
                return await _context.Todos
                    .AsNoTracking()
                    .FirstOrDefaultAsync(t => t.Id == id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving todo with ID {Id}", id);
                throw;
            }
        }

        public async Task<Todo> CreateTodoAsync(Todo todo)
        {
            try
            {
                todo.CreatedAt = DateTime.UtcNow;
                _context.Todos.Add(todo);
                await _context.SaveChangesAsync();
                
                _logger.LogInformation("Created todo with ID {Id}", todo.Id);
                return todo;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating todo");
                throw;
            }
        }

        public async Task<Todo?> UpdateTodoAsync(int id, Todo todo)
        {
            try
            {
                var existingTodo = await _context.Todos.FindAsync(id);
                if (existingTodo == null)
                {
                    return null;
                }

                existingTodo.Title = todo.Title;
                existingTodo.Done = todo.Done;
                existingTodo.Description = todo.Description;
                existingTodo.Category = todo.Category;
                existingTodo.Priority = todo.Priority;
                existingTodo.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                
                _logger.LogInformation("Updated todo with ID {Id}", id);
                return existingTodo;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating todo with ID {Id}", id);
                throw;
            }
        }

        public async Task<bool> DeleteTodoAsync(int id)
        {
            try
            {
                var todo = await _context.Todos.FindAsync(id);
                if (todo == null)
                {
                    return false;
                }

                _context.Todos.Remove(todo);
                await _context.SaveChangesAsync();
                
                _logger.LogInformation("Deleted todo with ID {Id}", id);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting todo with ID {Id}", id);
                throw;
            }
        }

        public async Task<IEnumerable<Todo>> GetTodosByCategoryAsync(string category)
        {
            try
            {
                return await _context.Todos
                    .AsNoTracking()
                    .Where(t => t.Category == category)
                    .OrderByDescending(t => t.CreatedAt)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving todos by category {Category}", category);
                throw;
            }
        }

        public async Task<IEnumerable<Todo>> GetPendingTodosAsync()
        {
            try
            {
                return await _context.Todos
                    .AsNoTracking()
                    .Where(t => !t.Done)
                    .OrderBy(t => t.Priority)
                    .ThenByDescending(t => t.CreatedAt)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving pending todos");
                throw;
            }
        }
    }
}
