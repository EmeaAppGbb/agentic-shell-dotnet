using System.Net;
using System.Text.Json;

namespace agentic_api.Middleware;

/// <summary>
/// Middleware for handling exceptions and providing consistent error responses
/// </summary>
public class ErrorHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ErrorHandlingMiddleware> _logger;

    public ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger)
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
            _logger.LogError(ex, "An unhandled exception occurred during request processing");
            await HandleExceptionAsync(context, ex);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var statusCode = exception switch
        {
            ArgumentNullException => HttpStatusCode.BadRequest,
            ArgumentException => HttpStatusCode.BadRequest,
            InvalidOperationException => HttpStatusCode.BadRequest,
            UnauthorizedAccessException => HttpStatusCode.Unauthorized,
            TimeoutException => HttpStatusCode.RequestTimeout,
            HttpRequestException => HttpStatusCode.ServiceUnavailable,
            _ => HttpStatusCode.InternalServerError
        };

        var errorResponse = new
        {
            error = new
            {
                message = GetUserFriendlyMessage(exception),
                type = exception.GetType().Name,
                statusCode = (int)statusCode,
                timestamp = DateTime.UtcNow,
                traceId = context.TraceIdentifier
            }
        };

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;

        var json = JsonSerializer.Serialize(errorResponse, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        });

        await context.Response.WriteAsync(json);
    }

    private static string GetUserFriendlyMessage(Exception exception)
    {
        return exception switch
        {
            ArgumentNullException => "A required parameter was missing from the request.",
            ArgumentException => "Invalid parameter value provided.",
            InvalidOperationException => "The operation cannot be performed at this time.",
            UnauthorizedAccessException => "You are not authorized to access this resource.",
            TimeoutException => "The request timed out. Please try again.",
            HttpRequestException => "Unable to connect to external service. Please try again later.",
            _ => "An unexpected error occurred. Please try again later."
        };
    }
}
