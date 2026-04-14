using System.ComponentModel.DataAnnotations;

namespace TaskFlow.Api.DTOs;


public record RegisterRequest(
    [Required, MaxLength(200)] string Name,
    [Required, EmailAddress, MaxLength(320)] string Email,
    [Required, MinLength(8)] string Password
);

public record LoginRequest(
    [Required, EmailAddress] string Email,
    [Required] string Password
);

public record AuthResponse(string Token, UserDto User);


public record UserDto(Guid Id, string Name, string Email);


public record CreateProjectRequest(
    [Required, MaxLength(500)] string Name,
    string? Description
);

public record UpdateProjectRequest(
    [MaxLength(500)] string? Name,
    string? Description
);

public record ProjectDto(
    Guid Id,
    string Name,
    string? Description,
    Guid OwnerId,
    DateTime CreatedAt
);

public record ProjectDetailDto(
    Guid Id,
    string Name,
    string? Description,
    Guid OwnerId,
    DateTime CreatedAt,
    IEnumerable<TaskDto> Tasks
);

public record ProjectListResponse(IEnumerable<ProjectDto> Projects);


public record CreateTaskRequest(
    [Required, MaxLength(500)] string Title,
    string? Description,
    string? Priority,
    Guid? AssigneeId,
    DateOnly? DueDate
);

public record UpdateTaskRequest(
    string? Title,
    string? Description,
    string? Status,
    string? Priority,
    Guid? AssigneeId,
    DateOnly? DueDate
);

public record TaskDto(
    Guid Id,
    string Title,
    string? Description,
    string Status,
    string Priority,
    Guid ProjectId,
    Guid? AssigneeId,
    Guid CreatedById,
    DateOnly? DueDate,
    DateTime CreatedAt,
    DateTime UpdatedAt
);

public record TaskListResponse(IEnumerable<TaskDto> Tasks);


public record ProjectStatsDto(
    Dictionary<string, int> TasksByStatus,
    Dictionary<string, int> TasksByAssignee
);


public record ValidationErrorResponse(string Error, Dictionary<string, string> Fields);


public record ErrorResponse(string Error);


public record PaginatedResponse<T>(IEnumerable<T> Items, int Page, int Limit, int Total);
