using System.ComponentModel.DataAnnotations;

namespace TaskFlow.Api.Models;

public enum TaskStatus
{
    Todo,
    InProgress,
    Done
}

public enum TaskPriority
{
    Low,
    Medium,
    High
}

public class TaskItem
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required, MaxLength(500)]
    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    public TaskStatus Status { get; set; } = TaskStatus.Todo;

    public TaskPriority Priority { get; set; } = TaskPriority.Medium;

    public Guid ProjectId { get; set; }

    public Guid? AssigneeId { get; set; }

    public Guid CreatedById { get; set; }

    public DateOnly? DueDate { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public Project Project { get; set; } = null!;
    public User? Assignee { get; set; }
    public User CreatedBy { get; set; } = null!;
}
