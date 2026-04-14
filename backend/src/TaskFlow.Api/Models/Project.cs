using System.ComponentModel.DataAnnotations;

namespace TaskFlow.Api.Models;

public class Project
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required, MaxLength(500)]
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public Guid OwnerId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    
    public User Owner { get; set; } = null!;
    public ICollection<TaskItem> Tasks { get; set; } = new List<TaskItem>();
}
