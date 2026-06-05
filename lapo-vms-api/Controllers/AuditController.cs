using lapo_vms_api.Helpers;
using lapo_vms_api.Interface;
using Microsoft.AspNetCore.Mvc;

namespace lapo_vms_api.Controllers;

[Route("api/audit-logs")]
[ApiController]
public class AuditController(
    IAuditService auditService,
    IWebHostEnvironment environment,
    ILogger<AuditController> logger) : ControllerBase
{
    private readonly IAuditService _auditService = auditService;
    private readonly IWebHostEnvironment _environment = environment;
    private readonly ILogger<AuditController> _logger = logger;

    [HttpGet]
    public async Task<IActionResult> GetAuditLogs([FromQuery] AuditQueryParameters queryParameters)
    {
        var logs = await _auditService.GetLogsAsync(queryParameters);
        return Ok(logs);
    }

    [HttpGet("export-file")]
    public IActionResult ExportLogFile([FromQuery] DateTime? date)
    {
        var logsDirectory = Path.Combine(_environment.ContentRootPath, "logs");
        if (!Directory.Exists(logsDirectory))
            return NotFound(new { message = "Log directory was not found." });

        var logFile = date.HasValue
            ? Path.Combine(logsDirectory, $"app-{date.Value:yyyyMMdd}.log")
            : Directory
                .EnumerateFiles(logsDirectory, "app-*.log", SearchOption.TopDirectoryOnly)
                .OrderByDescending(System.IO.File.GetLastWriteTimeUtc)
                .FirstOrDefault();

        if (string.IsNullOrWhiteSpace(logFile) || !System.IO.File.Exists(logFile))
            return NotFound(new { message = "Log file was not found." });

        var fileName = Path.GetFileName(logFile);
        var stream = new System.IO.FileStream(
            logFile,
            FileMode.Open,
            FileAccess.Read,
            FileShare.ReadWrite | FileShare.Delete);

        _logger.LogInformation("Log file exported. FileName={FileName}", fileName);

        return File(stream, "text/plain", fileName);
    }
}
