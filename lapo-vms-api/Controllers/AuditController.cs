using lapo_vms_api.Helpers;
using lapo_vms_api.Interface;
using Microsoft.AspNetCore.Mvc;

namespace lapo_vms_api.Controllers;

[Route("api/audit-logs")]
[ApiController]
public class AuditController(IAuditService auditService) : ControllerBase
{
    private readonly IAuditService _auditService = auditService;

    [HttpGet]
    public async Task<IActionResult> GetAuditLogs([FromQuery] AuditQueryParameters queryParameters)
    {
        var logs = await _auditService.GetLogsAsync(queryParameters);
        return Ok(logs);
    }
}
