using lapo_vms_api.Dtos.Visitor;
using lapo_vms_api.Dtos;
using lapo_vms_api.Helpers;
using lapo_vms_api.Interface;
using lapo_vms_api.Mappers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace lapo_vms_api.Controllers;

[Route("api/visitors")]
[ApiController]
public class VisitorController(
    IVisitorRepository visitorRepository,
    IExportService exportService,
    ILogger<VisitorController> logger) : ControllerBase
{
    private readonly IVisitorRepository _visitorRepository = visitorRepository;
    private readonly IExportService _exportService = exportService;
    private readonly ILogger<VisitorController> _logger = logger;

    /// <summary>
    /// Retrieves the list of registered visitors and applies any supplied query filters,
    /// paging, or search options before returning the result.
    /// </summary>
    /// <param name="queryParameters">
    /// Query string options used to filter, search, sort, or paginate the visitor list.
    /// </param>
    /// <returns>
    /// A collection of visitor records formatted as visitor DTOs.
    /// </returns>
    [HttpGet]
    public async Task<IActionResult> GetAllVisitors([FromQuery] QueryParameters queryParameters)
    {
        var visitors = await _visitorRepository.GetAllAsync(queryParameters);
        return Ok(visitors.Select(v => v.ToVisitorDto()));
    }

    /// <summary>
    /// Retrieves the details of a single visitor using the visitor's unique identifier.
    /// </summary>
    /// <param name="id">The unique ID of the visitor to retrieve.</param>
    /// <returns>
    /// The matching visitor record when found; otherwise a not found response.
    /// </returns>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById([FromRoute] Guid id)
    {
        var visitor = await _visitorRepository.GetByIdAsync(id);
        if (visitor == null)
        {
            return NotFound();
        }
        return Ok(visitor.ToVisitorDto());
    }

    /// <summary>
    /// Retrieves an existing visitor by exact phone number for returning visitor auto-fill.
    /// </summary>
    /// <param name="phoneNumber">The visitor phone number to match exactly after removing formatting.</param>
    /// <returns>
    /// The matching visitor record when found; otherwise a bad request or not found response.
    /// </returns>
    [HttpGet("phone")]
    [AllowAnonymous]
    public async Task<IActionResult> GetByPhoneNumber([FromQuery] string phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
            return BadRequest("Phone number is required.");

        var normalizedPhoneNumber = new string(phoneNumber.Where(char.IsDigit).ToArray());
        if (string.IsNullOrWhiteSpace(normalizedPhoneNumber))
            return BadRequest("Phone number must contain digits.");

        var visitor = await _visitorRepository.GetByPhoneNumberAsync(normalizedPhoneNumber);
        if (visitor == null) return NotFound();

        return Ok(visitor.ToVisitorDto());
    }

    /// <summary>
    /// Creates a new visitor record from multipart form data, uploads the supplied photo,
    /// and stores the generated photo path with the visitor details.
    /// </summary>
    /// <param name="dto">
    /// The visitor payload containing personal details, visitor type, and the photo file to upload.
    /// </param>
    /// <returns>
    /// The newly created visitor record when validation succeeds; otherwise a bad request response.
    /// </returns>
    [HttpPost]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> CreateVisitor([FromBody] CreateVisitorDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var photoPath = await ImageUploader.UploadImage(dto.Photo);

        var visitor = dto.ToVisitorFromCreateDto();
        visitor.PhotoPath = photoPath;

        var created = await _visitorRepository.CreateAsync(visitor);

        _logger.LogInformation(
            "Visitor created. VisitorId={VisitorId} ActorId={ActorId} StaffId={StaffId} Role={Role}",
            created.Id,
            User.FindFirstValue(ClaimTypes.NameIdentifier),
            User.FindFirstValue("staffId"),
            User.FindFirstValue(ClaimTypes.Role));

        return Ok(created.ToVisitorDto());
    }

    [HttpGet("export")]
    public async Task<IActionResult> ExportVisitors([FromQuery] VisitorExportRequest request)
    {
        var visitorsForExport = await _visitorRepository.GetVisitorsForExportAsync(request);
        if (!visitorsForExport.Any())
        {
            LogExportFailure("NoRecordsFound");
            return NotFound("No visitors found for the specified export criteria.");
        }

        byte[] fileContent;
        string fileName;

        if (request.Format == ExportType.Csv)
        {
            visitorsForExport.ForEach(v => v.PhoneNumber = $"=\"{v.PhoneNumber}\"");
            fileContent = _exportService.ExportToCsv(visitorsForExport);
            fileName = $"Visitor_Export_{WatClock.Now:yyyyMMddHHmmss}.csv";

            LogExport(request.Format, visitorsForExport.Count);
            return File(fileContent, "text/csv", fileName);
        }

        if (request.Format == ExportType.Excel)
        {
            fileContent = _exportService.ExportToExcel(visitorsForExport, "Visitors");
            fileName = $"Visitor_Export_{WatClock.Now:yyyyMMddHHmmss}.xlsx";

            LogExport(request.Format, visitorsForExport.Count);
            return File(
                fileContent,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileName
            );
        }

        LogExportFailure("InvalidFormat");
        return BadRequest("Invalid export format.");
    }

    private void LogExport(ExportType format, int recordCount)
    {
        _logger.LogInformation(
            "Visitors exported. Format={Format} RecordCount={RecordCount} ActorId={ActorId} StaffId={StaffId} Role={Role}",
            format,
            recordCount,
            User.FindFirstValue(ClaimTypes.NameIdentifier),
            User.FindFirstValue("staffId"),
            User.FindFirstValue(ClaimTypes.Role));
    }

    private void LogExportFailure(string reason)
    {
        _logger.LogWarning(
            "Visitor export failed. Reason={Reason} ActorId={ActorId} StaffId={StaffId} Role={Role}",
            reason,
            User.FindFirstValue(ClaimTypes.NameIdentifier),
            User.FindFirstValue("staffId"),
            User.FindFirstValue(ClaimTypes.Role));
    }
}
