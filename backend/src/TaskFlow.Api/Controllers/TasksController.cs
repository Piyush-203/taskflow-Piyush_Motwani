using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TaskFlow.Api.Data;
using TaskFlow.Api.DTOs;
using TaskFlow.Api.Models;
using TaskStatus = TaskFlow.Api.Models.TaskStatus;

namespace TaskFlow.Api.Controllers;

[ApiController]
[Authorize]
public class TasksController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ILogger<TasksController> _logger;

    public TasksController(AppDbContext db, ILogger<TasksController> logger)
    {
        _db = db;
        _logger = logger;
    }

    private Guid CurrentUserId =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub")
            ?? throw new InvalidOperationException("No user id in token"));

    
    [HttpGet("projects/{projectId:guid}/tasks")]
    [ProducesResponseType(typeof(TaskListResponse), 200)]
    [ProducesResponseType(typeof(ErrorResponse), 404)]
    public async Task<IActionResult> ListTasks(
        Guid projectId,
        [FromQuery] string? status,
        [FromQuery] Guid? assignee,
        [FromQuery] int page = 1,
        [FromQuery] int limit = 20)
    {
        var projectExists = await _db.Projects.AnyAsync(p => p.Id == projectId);
        if (!projectExists) return NotFound(new ErrorResponse("not found"));

        var query = _db.Tasks.Where(t => t.ProjectId == projectId);

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<TaskStatus>(status, ignoreCase: true, out var parsedStatus))
            query = query.Where(t => t.Status == parsedStatus);

        if (assignee.HasValue)
            query = query.Where(t => t.AssigneeId == assignee);

        var total = await query.CountAsync();

        var tasks = await query
            .OrderByDescending(t => t.CreatedAt)
            .Skip((page - 1) * limit)
            .Take(limit)
            .ToListAsync();

        return Ok(new { tasks = tasks.Select(MapTask), page, limit, total });
    }

    
    [HttpPost("projects/{projectId:guid}/tasks")]
    [ProducesResponseType(typeof(TaskDto), 201)]
    [ProducesResponseType(typeof(ValidationErrorResponse), 400)]
    [ProducesResponseType(typeof(ErrorResponse), 404)]
    public async Task<IActionResult> CreateTask(Guid projectId, [FromBody] CreateTaskRequest req)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState.ToErrorResponse());

        var projectExists = await _db.Projects.AnyAsync(p => p.Id == projectId);
        if (!projectExists) return NotFound(new ErrorResponse("not found"));

        TaskPriority priority = TaskPriority.Medium;
        if (!string.IsNullOrWhiteSpace(req.Priority) && !Enum.TryParse(req.Priority, ignoreCase: true, out priority))
            return BadRequest(new ValidationErrorResponse("validation failed",
                new Dictionary<string, string> { ["priority"] = "must be low, medium, or high" }));

        
        if (req.AssigneeId.HasValue)
        {
            var assigneeExists = await _db.Users.AnyAsync(u => u.Id == req.AssigneeId);
            if (!assigneeExists)
                return BadRequest(new ValidationErrorResponse("validation failed",
                new Dictionary<string, string> { ["assignee_id"] = "user not found" }));
        }

        var task = new TaskItem
        {
            Title = req.Title.Trim(),
            Description = req.Description?.Trim(),
            Priority = priority,
            ProjectId = projectId,
            AssigneeId = req.AssigneeId,
            CreatedById = CurrentUserId,
            DueDate = req.DueDate
        };

        _db.Tasks.Add(task);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Task created: {TaskId} in project {ProjectId}", task.Id, projectId);
        return StatusCode(201, MapTask(task));
    }

    
    [HttpPatch("tasks/{id:guid}")]
    [ProducesResponseType(typeof(TaskDto), 200)]
    [ProducesResponseType(typeof(ValidationErrorResponse), 400)]
    [ProducesResponseType(typeof(ErrorResponse), 404)]
    public async Task<IActionResult> UpdateTask(Guid id, [FromBody] UpdateTaskRequest req)
    {
        var task = await _db.Tasks.Include(t => t.Project).FirstOrDefaultAsync(t => t.Id == id);
        if (task is null) return NotFound(new ErrorResponse("not found"));

        if (req.Title is not null) task.Title = req.Title.Trim();
        if (req.Description is not null) task.Description = req.Description.Trim();
        if (req.DueDate.HasValue) task.DueDate = req.DueDate;

        if (!string.IsNullOrWhiteSpace(req.Status))
        {
            if (!Enum.TryParse<TaskStatus>(req.Status, ignoreCase: true, out var parsedStatus))
                return BadRequest(new ValidationErrorResponse("validation failed",
                new Dictionary<string, string> { ["status"] = "must be todo, in_progress, or done" }));
            task.Status = parsedStatus;
        }

        if (!string.IsNullOrWhiteSpace(req.Priority))
        {
            if (!Enum.TryParse<TaskPriority>(req.Priority, ignoreCase: true, out var parsedPriority))
                return BadRequest(new ValidationErrorResponse("validation failed",
                new Dictionary<string, string> { ["priority"] = "must be low, medium, or high" }));
            task.Priority = parsedPriority;
        }

        if (req.AssigneeId.HasValue)
        {
            var assigneeExists = await _db.Users.AnyAsync(u => u.Id == req.AssigneeId);
            if (!assigneeExists)
                return BadRequest(new ValidationErrorResponse("validation failed",
                new Dictionary<string, string> { ["assignee_id"] = "user not found" }));
            task.AssigneeId = req.AssigneeId;
        }

        task.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return Ok(MapTask(task));
    }

    
    [HttpDelete("tasks/{id:guid}")]
    [ProducesResponseType(204)]
    [ProducesResponseType(typeof(ErrorResponse), 403)]
    [ProducesResponseType(typeof(ErrorResponse), 404)]
    public async Task<IActionResult> DeleteTask(Guid id)
    {
        var task = await _db.Tasks.Include(t => t.Project).FirstOrDefaultAsync(t => t.Id == id);
        if (task is null) return NotFound(new ErrorResponse("not found"));

        var userId = CurrentUserId;
        if (task.CreatedById != userId && task.Project.OwnerId != userId)
            return StatusCode(403, new ErrorResponse("forbidden"));

        _db.Tasks.Remove(task);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Task deleted: {TaskId} by {UserId}", id, userId);
        return NoContent();
    }

    private static TaskDto MapTask(TaskItem t) => new(
        t.Id, t.Title, t.Description,
        t.Status.ToString(), t.Priority.ToString(),
        t.ProjectId, t.AssigneeId, t.CreatedById,
        t.DueDate, t.CreatedAt, t.UpdatedAt
    );
}
