using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TaskFlow.Api.Data;
using TaskFlow.Api.DTOs;
using TaskFlow.Api.Models;

namespace TaskFlow.Api.Controllers;

[ApiController]
[Route("projects")]
[Authorize]
public class ProjectsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ILogger<ProjectsController> _logger;

    public ProjectsController(AppDbContext db, ILogger<ProjectsController> logger)
    {
        _db = db;
        _logger = logger;
    }

    private Guid CurrentUserId =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub")
            ?? throw new InvalidOperationException("No user id in token"));

    
    [HttpGet]
    [ProducesResponseType(typeof(ProjectListResponse), 200)]
    public async Task<IActionResult> ListProjects([FromQuery] int page = 1, [FromQuery] int limit = 20)
    {
        var userId = CurrentUserId;

        var query = _db.Projects
            .Where(p => p.OwnerId == userId
                || p.Tasks.Any(t => t.AssigneeId == userId || t.CreatedById == userId))
            .OrderByDescending(p => p.CreatedAt);

        var total = await query.CountAsync();

        var projects = await query
            .Skip((page - 1) * limit)
            .Take(limit)
            .Select(p => new ProjectDto(p.Id, p.Name, p.Description, p.OwnerId, p.CreatedAt))
            .ToListAsync();

        return Ok(new { projects, page, limit, total });
    }


    [HttpPost]
    [ProducesResponseType(typeof(ProjectDto), 201)]
    [ProducesResponseType(typeof(ValidationErrorResponse), 400)]
    public async Task<IActionResult> CreateProject([FromBody] CreateProjectRequest req)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState.ToErrorResponse());

        var project = new Project
        {
            Name = req.Name.Trim(),
            Description = req.Description?.Trim(),
            OwnerId = CurrentUserId
        };

        _db.Projects.Add(project);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Project created: {ProjectId} by {UserId}", project.Id, CurrentUserId);

        return StatusCode(201, new ProjectDto(project.Id, project.Name, project.Description, project.OwnerId, project.CreatedAt));
    }

    
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ProjectDetailDto), 200)]
    [ProducesResponseType(typeof(ErrorResponse), 404)]
    public async Task<IActionResult> GetProject(Guid id)
    {
        var project = await _db.Projects
            .Include(p => p.Tasks)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (project is null)
            return NotFound(new ErrorResponse("not found"));

        var tasks = project.Tasks.Select(MapTask);
        return Ok(new ProjectDetailDto(project.Id, project.Name, project.Description, project.OwnerId, project.CreatedAt, tasks));
    }

    
    [HttpPatch("{id:guid}")]
    [ProducesResponseType(typeof(ProjectDto), 200)]
    [ProducesResponseType(typeof(ErrorResponse), 403)]
    [ProducesResponseType(typeof(ErrorResponse), 404)]
    public async Task<IActionResult> UpdateProject(Guid id, [FromBody] UpdateProjectRequest req)
    {
        var project = await _db.Projects.FindAsync(id);
        if (project is null) return NotFound(new ErrorResponse("not found"));
        if (project.OwnerId != CurrentUserId) return StatusCode(403, new ErrorResponse("forbidden"));

        if (req.Name is not null) project.Name = req.Name.Trim();
        if (req.Description is not null) project.Description = req.Description.Trim();

        await _db.SaveChangesAsync();
        return Ok(new ProjectDto(project.Id, project.Name, project.Description, project.OwnerId, project.CreatedAt));
    }

    
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(204)]
    [ProducesResponseType(typeof(ErrorResponse), 403)]
    [ProducesResponseType(typeof(ErrorResponse), 404)]
    public async Task<IActionResult> DeleteProject(Guid id)
    {
        var project = await _db.Projects.FindAsync(id);
        if (project is null) return NotFound(new ErrorResponse("not found"));
        if (project.OwnerId != CurrentUserId) return StatusCode(403, new ErrorResponse("forbidden"));

        _db.Projects.Remove(project);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Project deleted: {ProjectId} by {UserId}", id, CurrentUserId);
        return NoContent();
    }

    
    [HttpGet("{id:guid}/stats")]
    [ProducesResponseType(typeof(ProjectStatsDto), 200)]
    [ProducesResponseType(typeof(ErrorResponse), 404)]
    public async Task<IActionResult> GetProjectStats(Guid id)
    {
        var exists = await _db.Projects.AnyAsync(p => p.Id == id);
        if (!exists) return NotFound(new ErrorResponse("not found"));

        var tasks = await _db.Tasks
            .Where(t => t.ProjectId == id)
            .Include(t => t.Assignee)
            .ToListAsync();

        var byStatus = tasks
            .GroupBy(t => t.Status.ToString())
            .ToDictionary(g => g.Key, g => g.Count());

        var byAssignee = tasks
            .Where(t => t.AssigneeId.HasValue)
            .GroupBy(t => t.Assignee?.Name ?? t.AssigneeId.ToString()!)
            .ToDictionary(g => g.Key, g => g.Count());

        return Ok(new ProjectStatsDto(byStatus, byAssignee));
    }

    private static TaskDto MapTask(TaskItem t) => new(
        t.Id, t.Title, t.Description,
        t.Status.ToString(), t.Priority.ToString(),
        t.ProjectId, t.AssigneeId, t.CreatedById,
        t.DueDate, t.CreatedAt, t.UpdatedAt
    );
}
