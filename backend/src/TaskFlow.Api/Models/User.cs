using System.ComponentModel.DataAnnotations;

namespace TaskFlow.Api.Models;

public class User
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required, MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required, MaxLength(320)]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string PasswordHash { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

  
    public ICollection<Project> OwnedProjects { get; set; } = new List<Project>();
    public ICollection<TaskItem> AssignedTasks { get; set; } = new List<TaskItem>();
}
