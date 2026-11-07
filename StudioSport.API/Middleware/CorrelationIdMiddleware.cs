using Microsoft.Extensions.Primitives;

namespace StudioSport.API.Middleware;

public class CorrelationIdMiddleware
{
    private const string HeaderName = "X-Correlation-Id";
    private readonly RequestDelegate _next;
    private readonly ILogger<CorrelationIdMiddleware> _logger;

    public CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // אם הלקוח שלח מזהה - משתמשים בו; אחרת מייצרים חדש
        var correlationId = context.Request.Headers.TryGetValue(HeaderName, out StringValues value)
            && !string.IsNullOrWhiteSpace(value)
                ? value.ToString()
                : Guid.NewGuid().ToString();

        context.Items[HeaderName] = correlationId;
        context.Response.Headers[HeaderName] = correlationId;

        // מזרים ללוג את כל שורות הבקשה הזו
        using (_logger.BeginScope(new Dictionary<string, object>
        {
            ["CorrelationId"] = correlationId
        }))
        {
            _logger.LogInformation(
                "Incoming request {Method} {Path}",
                context.Request.Method, context.Request.Path);

            await _next(context);
        }
    }
}