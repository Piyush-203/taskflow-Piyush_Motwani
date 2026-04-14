using Microsoft.EntityFrameworkCore;
using TaskFlow.Api.Models;

namespace TaskFlow.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<TaskItem> Tasks => Set<TaskItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        
        modelBuilder.Entity<User>(e =>
        {
            e.HasKey(u => u.Id);
            e.HasIndex(u => u.Email).IsUnique();
            e.Property(u => u.Email).HasMaxLength(320).IsRequired();
            e.Property(u => u.Name).HasMaxLength(200).IsRequired();
        });

        
        modelBuilder.Entity<Project>(e =>
        {
            e.HasKey(p => p.Id);
            e.Property(p => p.Name).HasMaxLength(500).IsRequired();
            e.HasOne(p => p.Owner)
                .WithMany(u => u.OwnedProjects)
                .HasForeignKey(p => p.OwnerId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasMany(p => p.Tasks)
                .WithOne(t => t.Project)
                .HasForeignKey(t => t.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        
        modelBuilder.Entity<TaskItem>(e =>
        {
            e.HasKey(t => t.Id);
            e.Property(t => t.Title).HasMaxLength(500).IsRequired();
            e.Property(t => t.Status)
                .HasConversion<string>();
            e.Property(t => t.Priority)
                .HasConversion<string>();
            e.HasOne(t => t.Assignee)
                .WithMany(u => u.AssignedTasks)
                .HasForeignKey(t => t.AssigneeId)
                .OnDelete(DeleteBehavior.SetNull);
            e.HasOne(t => t.CreatedBy)
                .WithMany()
                .HasForeignKey(t => t.CreatedById)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
