using EventTicketing.BusinessLogic.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace EventTicketing.Api.Middleware;

/// <summary>Translates domain exceptions into RFC-7807 ProblemDetails with the right status code.</summary>
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            var (status, title) = ex switch
            {
                NotFoundException => (StatusCodes.Status404NotFound, "Resource not found"),
                ValidationException => (StatusCodes.Status400BadRequest, "Invalid request"),
                ConflictException => (StatusCodes.Status409Conflict, "Conflict"),
                _ => (StatusCodes.Status500InternalServerError, "An unexpected error occurred")
            };

            if (status == StatusCodes.Status500InternalServerError)
                _logger.LogError(ex, "Unhandled exception");

            var problem = new ProblemDetails
            {
                Status = status,
                Title = title,
                // Internal errors hide their message; expected domain errors are safe to surface.
                Detail = status == StatusCodes.Status500InternalServerError ? null : ex.Message
            };

            context.Response.StatusCode = status;
            await context.Response.WriteAsJsonAsync(problem);
        }
    }
}
