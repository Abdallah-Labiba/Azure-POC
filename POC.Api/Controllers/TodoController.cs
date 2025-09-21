using Microsoft.AspNetCore.Mvc;
using POC.Api.Models;
using POC.Api.Services;

namespace POC.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TodoController : ControllerBase
    {
        private readonly ITodoService _todoService;
        private readonly IMessageQueueService _messageQueueService;
        private readonly ILogger<TodoController> _logger;

        public TodoController(
            ITodoService todoService,
            IMessageQueueService messageQueueService,
            ILogger<TodoController> logger)
        {
            _todoService = todoService;
            _messageQueueService = messageQueueService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Todo>>> GetTodos()
        {
            try
            {
                var todos = await _todoService.GetAllTodosAsync();
                return Ok(todos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving todos");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Todo>> GetTodo(int id)
        {
            try
            {
                var todo = await _todoService.GetTodoByIdAsync(id);
                if (todo == null)
                {
                    return NotFound();
                }
                return Ok(todo);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving todo with ID {Id}", id);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPost]
        public async Task<ActionResult<Todo>> CreateTodo(Todo todo)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var createdTodo = await _todoService.CreateTodoAsync(todo);
                
                // Publish message to queue
                await _messageQueueService.PublishMessageAsync(
                    $"New todo created: {createdTodo.Title}",
                    "todo-created",
                    "todo");

                return CreatedAtAction(nameof(GetTodo), new { id = createdTodo.Id }, createdTodo);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating todo");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateTodo(int id, Todo todo)
        {
            try
            {
                if (id != todo.Id)
                {
                    return BadRequest("ID mismatch");
                }

                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var updatedTodo = await _todoService.UpdateTodoAsync(id, todo);
                if (updatedTodo == null)
                {
                    return NotFound();
                }

                // Publish message to queue
                await _messageQueueService.PublishMessageAsync(
                    $"Todo updated: {updatedTodo.Title}",
                    "todo-updated",
                    "todo");

                return Ok(updatedTodo);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating todo with ID {Id}", id);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTodo(int id)
        {
            try
            {
                var result = await _todoService.DeleteTodoAsync(id);
                if (!result)
                {
                    return NotFound();
                }

                // Publish message to queue
                await _messageQueueService.PublishMessageAsync(
                    $"Todo deleted with ID: {id}",
                    "todo-deleted",
                    "todo");

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting todo with ID {Id}", id);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet("category/{category}")]
        public async Task<ActionResult<IEnumerable<Todo>>> GetTodosByCategory(string category)
        {
            try
            {
                var todos = await _todoService.GetTodosByCategoryAsync(category);
                return Ok(todos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving todos by category {Category}", category);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet("pending")]
        public async Task<ActionResult<IEnumerable<Todo>>> GetPendingTodos()
        {
            try
            {
                var todos = await _todoService.GetPendingTodosAsync();
                return Ok(todos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving pending todos");
                return StatusCode(500, "Internal server error");
            }
        }
    }
}
