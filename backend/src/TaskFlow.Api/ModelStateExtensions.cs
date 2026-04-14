using Microsoft.AspNetCore.Mvc.ModelBinding;
using TaskFlow.Api.DTOs;

namespace TaskFlow.Api;

public static class ModelStateExtensions
{
    public static ValidationErrorResponse ToErrorResponse(this ModelStateDictionary modelState)
    {
        var fields = modelState
            .Where(kv => kv.Value?.Errors.Any() == true)
            .ToDictionary(
                kv => kv.Key,
                kv => kv.Value!.Errors.First().ErrorMessage
            );

        return new ValidationErrorResponse("validation failed", fields);
    }
}
