using System.Data.Common;
using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Serilog.Context;

namespace lapo_vms_api.Middleware;

public class TraceLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<TraceLoggingMiddleware> _logger;
    private readonly int _slowRequestThresholdMs;

    public TraceLoggingMiddleware(
        RequestDelegate next,
        ILogger<TraceLoggingMiddleware> logger,
        IConfiguration configuration)
    {
        _next = next;
        _logger = logger;
        _slowRequestThresholdMs = configuration.GetValue<int?>("Logging:SlowRequestThresholdMs") ?? 2000;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        var path = context.Request.Path.Value ?? string.Empty;

        using (LogContext.PushProperty("TraceId", context.TraceIdentifier))
        using (LogContext.PushProperty("Method", context.Request.Method))
        using (LogContext.PushProperty("Path", path))
        {
            var exceptionLogged = false;

            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                var errorSource = IsDatabaseException(ex) ? "Database" : "API";

                using (LogContext.PushProperty("StatusCode", StatusCodes.Status500InternalServerError))
                using (LogContext.PushProperty("ErrorSource", errorSource))
                {
                    _logger.LogError(
                        ex,
                        "{ErrorSource} error after {ElapsedMs}ms. ExceptionType={ExceptionType}. Message={ErrorMessage}",
                        errorSource,
                        stopwatch.ElapsedMilliseconds,
                        ex.GetType().Name,
                        ex.Message);
                }

                exceptionLogged = true;
                throw;
            }
            finally
            {
                stopwatch.Stop();

                if (!exceptionLogged)
                {
                    LogCompletedRequest(context.Response.StatusCode, stopwatch.ElapsedMilliseconds);
                }
            }
        }
    }

    private void LogCompletedRequest(int statusCode, long elapsedMs)
    {
        if (statusCode >= StatusCodes.Status500InternalServerError)
        {
            _logger.LogError(
                "Request completed with server error. StatusCode={StatusCode} ElapsedMs={ElapsedMs}",
                statusCode,
                elapsedMs);
        }
        else if (statusCode is StatusCodes.Status401Unauthorized or StatusCodes.Status403Forbidden)
        {
            _logger.LogWarning(
                "Request access denied. StatusCode={StatusCode} ElapsedMs={ElapsedMs}",
                statusCode,
                elapsedMs);
        }

        if (elapsedMs >= _slowRequestThresholdMs)
        {
            _logger.LogWarning(
                "Slow request completed. StatusCode={StatusCode} ElapsedMs={ElapsedMs} ThresholdMs={ThresholdMs}",
                statusCode,
                elapsedMs,
                _slowRequestThresholdMs);
        }
    }

    private static bool IsDatabaseException(Exception exception)
    {
        for (var current = exception; current != null; current = current.InnerException)
        {
            if (current is DbException
                || current is DbUpdateException
                || current.GetType().FullName == "Microsoft.Data.SqlClient.SqlException")
            {
                return true;
            }
        }

        return false;
    }
}
