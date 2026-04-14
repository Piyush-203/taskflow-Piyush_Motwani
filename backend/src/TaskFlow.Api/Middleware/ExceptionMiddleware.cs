using System.Net;
using System.Text.Json;
using TaskFlow.Api.DTOs;

namespace TaskFlow.Api.Middleware;

public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;

    public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext ctx)
    {
        try
        {
            await _next(ctx);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception for {Method} {Path}", ctx.Request.Method, ctx.Request.Path);
            ctx.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            ctx.Response.ContentType = "application/json";

            var body = JsonSerializer.Serialize(new ErrorResponse("internal server error"));
            await ctx.Response.WriteAsync(body);
        }
    }
}
