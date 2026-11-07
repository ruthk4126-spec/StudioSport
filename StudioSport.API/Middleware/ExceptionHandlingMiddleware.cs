using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using StudioSport.Service.Exceptions;

namespace StudioSport.API.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger)
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
            await HandleAsync(context, ex);
        }
    }

    private async Task HandleAsync(HttpContext context, Exception ex)
    {
        var statusCode = ex switch
        {
            NotFoundException => StatusCodes.Status404NotFound,
            ForbiddenException => StatusCodes.Status403Forbidden,
            ConflictException => StatusCodes.Status409Conflict,
            DbUpdateConcurrencyException => StatusCodes.Status409Conflict,
            DbUpdateException => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status500InternalServerError
        };

        var correlationId = context.Items["X-Correlation-Id"]?.ToString();

        if (statusCode == StatusCodes.Status500InternalServerError)
            _logger.LogError(ex, "Unhandled exception. CorrelationId {CorrelationId}", correlationId);
        else
            _logger.LogWarning(ex, "Handled exception {Type}. CorrelationId {CorrelationId}",
                ex.GetType().Name, correlationId);

        // התשובה כבר החלה להישלח — אין מה לכתוב, רק לזרוק הלאה
        if (context.Response.HasStarted)
            throw ex;

        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";

        var message = ex switch
        {
            DbUpdateConcurrencyException =>
                "הנתון עודכן על ידי משתמש אחר. יש לרענן ולנסות שוב.",
            DbUpdateException =>
                "לא ניתן להשלים את הפעולה כרגע. ייתכן שהמקום נתפס בינתיים.",
            _ when statusCode == StatusCodes.Status500InternalServerError =>
                "אירעה שגיאה בלתי צפויה",
            _ => ex.Message
        };

        var payload = new
        {
            status = statusCode,
            message,
            correlationId
        };

        await context.Response.WriteAsync(
            JsonSerializer.Serialize(payload, new JsonSerializerOptions
            {
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            }));
    }
}