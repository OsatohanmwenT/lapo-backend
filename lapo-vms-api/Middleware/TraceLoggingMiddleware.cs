using System.Data.Common;
using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Serilog.Context;

namespace lapo_vms_api.Middleware;

public class TraceLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<TraceLoggingMiddleware> _logger;

    public TraceLoggingMiddleware(RequestDelegate next, ILogger<TraceLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        var path = context.Request.Path.Value ?? string.Empty;

        using (LogContext.PushProperty("TraceId", context.TraceIdentifier))
        using (LogContext.PushProperty("Method", context.Request.Method))
        using (LogContext.PushProperty("Path", path))
        {
            _logger.LogTrace("Request started");

            try
            {
                await _next(context);

                stopwatch.Stop();

                using (LogContext.PushProperty("StatusCode", context.Response.StatusCode))
                {
                    _logger.LogTrace(
                        "Request completed in {ElapsedMs}ms",
                        stopwatch.ElapsedMilliseconds);
                }
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

                throw;
            }
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
