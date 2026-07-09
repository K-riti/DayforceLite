using System.Net;
using System.Text.Json;
using DayforceLite.Core.Exceptions;

namespace DayforceLite.API.Middleware;

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
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var response = context.Response;
        response.ContentType = "application/json";

        var (statusCode, message, errors) = exception switch
        {
            NotFoundException notFound => (HttpStatusCode.NotFound, notFound.Message, null as object),
            ValidationException validation => (HttpStatusCode.BadRequest, validation.Message, validation.Errors),
            ArgumentException argument => (HttpStatusCode.BadRequest, argument.Message, null),
            InvalidOperationException invalid => (HttpStatusCode.BadRequest, invalid.Message, null),
            UnauthorizedAccessException => (HttpStatusCode.Unauthorized, "Unauthorized access", null),
            _ => (HttpStatusCode.InternalServerError, "An internal server error occurred", null)
        };

        // Log based on severity
        if (statusCode == HttpStatusCode.InternalServerError)
        {
            _logger.LogError(exception, "An unhandled exception occurred: {Message}", exception.Message);
        }
        else
        {
            _logger.LogWarning("Request failed with {StatusCode}: {Message}", statusCode, message);
        }

        response.StatusCode = (int)statusCode;

        var errorResponse = new ErrorResponse
        {
            StatusCode = response.StatusCode,
            Message = message,
            Errors = errors,
            Timestamp = DateTime.UtcNow,
            TraceId = context.TraceIdentifier
        };

        var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        await response.WriteAsync(JsonSerializer.Serialize(errorResponse, options));
    }
}

public class ErrorResponse
{
    public int StatusCode { get; set; }
    public string Message { get; set; } = string.Empty;
    public object? Errors { get; set; }
    public DateTime Timestamp { get; set; }
    public string TraceId { get; set; } = string.Empty;
}

public static class ExceptionHandlingMiddlewareExtensions
{
    public static IApplicationBuilder UseExceptionHandling(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<ExceptionHandlingMiddleware>();
    }
}
