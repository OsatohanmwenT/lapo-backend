using System.Security.Claims;
using lapo_vms_api.Interface;
using lapo_vms_api.Model;

namespace lapo_vms_api.Middleware;

public class AuditMiddleware
{
    private readonly RequestDelegate _next;

    public AuditMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, IAuditService auditService)
    {
        await _next(context);

        if (context.Response.StatusCode < 200 || context.Response.StatusCode >= 300)
            return;

        var path = context.Request.Path.Value?.ToLowerInvariant() ?? "";

        string? eventType = path switch
        {
            var p when p.Contains("/approve") => "APPROVE",
            var p when p.Contains("/signout") => "SIGN_OUT",
            _ => null
        };

        if (eventType == null)
            return;

        Guid? actorId = null;
        var actorIdClaim = context.User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (Guid.TryParse(actorIdClaim, out var parsedActorId))
            actorId = parsedActorId;

        var log = new AuditLog
        {
            EventType = eventType,
            ActorId = actorId,
            ActorRole = context.User.FindFirstValue(ClaimTypes.Role),
            Timestamp = DateTime.UtcNow,
            Metadata = $"Method: {context.Request.Method}; Path: {context.Request.Path}"
        };

        await auditService.LogEventAsync(log);
    }
}
