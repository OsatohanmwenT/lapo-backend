using lapo_vms_api.Data;
using lapo_vms_api.Helpers;
using lapo_vms_api.Interface;
using lapo_vms_api.Model;
using Microsoft.EntityFrameworkCore;

namespace lapo_vms_api.Services;

public class AuditService : IAuditService
{
    private readonly ApplicationDBContext _context;

    public AuditService(ApplicationDBContext context)
    {
        _context = context;
    }

    public async Task<List<AuditLog>> GetLogsAsync(AuditQueryParameters queryParameters)
    {
        var query = _context.AuditLogs.AsQueryable();

        if (queryParameters.StartDate.HasValue)
            query = query.Where(l => l.Timestamp >= queryParameters.StartDate.Value);

        if (queryParameters.EndDate.HasValue)
            query = query.Where(l => l.Timestamp <= queryParameters.EndDate.Value);

        if (!string.IsNullOrWhiteSpace(queryParameters.EventType))
            query = query.Where(l => l.EventType == queryParameters.EventType);

        return await query
            .OrderByDescending(l => l.Timestamp)
            .Skip((queryParameters.PageNumber - 1) * queryParameters.PageSize)
            .Take(queryParameters.PageSize)
            .ToListAsync();
    }

    public async Task LogEventAsync(AuditLog log)
    {
        var lastLog = await _context.AuditLogs
            .OrderByDescending(x => x.Timestamp)
            .FirstOrDefaultAsync();

        var prevHash = lastLog?.CurrentHash ?? "";

        var raw = string.Join("|",
                            log.Id,
                            log.EventType,
                            log.ActorId,
                            log.ActorRole,
                            log.VisitorId,
                            log.Timestamp.ToUniversalTime().ToString("O"),
                            log.Metadata,
                            prevHash
                        );

        var currentHash = AuditHelper.ComputeHash(raw);

        log.PrevHash = prevHash;
        log.CurrentHash = currentHash;

        _context.AuditLogs.Add(log);
        await _context.SaveChangesAsync();
    }
}