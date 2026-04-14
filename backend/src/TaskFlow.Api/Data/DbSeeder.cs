using Microsoft.EntityFrameworkCore;
using TaskFlow.Api.Models;

namespace TaskFlow.Api.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(AppDbContext db, ILogger logger)
    {
        
        await db.Database.EnsureCreatedAsync();

        if (await db.Users.AnyAsync())
        {
            logger.LogInformation("Database already seeded, skipping");
            return;
        }

        logger.LogInformation("Seeding database...");

        var user = new User
        {
            Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
            Name = "Test User",
            Email = "test@example.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("password123", workFactor: 12)
        };

        var user2 = new User
        {
            Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
            Name = "Jane Smith",
            Email = "jane@example.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("password123", workFactor: 12)
        };

        db.Users.AddRange(user, user2);
        await db.SaveChangesAsync();

        var project = new Project
        {
            Id = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
            Name = "Website Redesign",
            Description = "Q2 redesign of the company website",
            OwnerId = user.Id
        };

        db.Projects.Add(project);
        await db.SaveChangesAsync();

        db.Tasks.AddRange(
            new TaskItem
            {
                Title = "Design new homepage layout",
                Description = "Create wireframes and mockups for the new homepage",
                Status = Models.TaskStatus.Todo,
                Priority = TaskPriority.High,
                ProjectId = project.Id,
                CreatedById = user.Id,
                AssigneeId = user.Id,
                DueDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(14))
            },
            new TaskItem
            {
                Title = "Set up CI/CD pipeline",
                Description = "Configure GitHub Actions for automated deployment",
                Status = Models.TaskStatus.InProgress,
                Priority = TaskPriority.Medium,
                ProjectId = project.Id,
                CreatedById = user.Id,
                AssigneeId = user2.Id,
                DueDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(7))
            },
            new TaskItem
            {
                Title = "Write API documentation",
                Description = "Document all REST endpoints with examples",
                Status = Models.TaskStatus.Done,
                Priority = TaskPriority.Low,
                ProjectId = project.Id,
                CreatedById = user2.Id,
                AssigneeId = null
            }
        );

        await db.SaveChangesAsync();
        logger.LogInformation("Seeding complete");
    }
}
