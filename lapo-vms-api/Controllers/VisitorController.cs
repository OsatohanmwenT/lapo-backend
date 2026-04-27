using lapo_vms_api.Dtos.Visitor;
using lapo_vms_api.Dtos;
using lapo_vms_api.Helpers;
using lapo_vms_api.Interface;
using lapo_vms_api.Mappers;
using Microsoft.AspNetCore.Mvc;

namespace lapo_vms_api.Controllers;

[Route("api/visitors")]
[ApiController]
public class VisitorController(IVisitorRepository visitorRepository, IExportService exportService) : ControllerBase
{
    private readonly IVisitorRepository _visitorRepository = visitorRepository;
    private readonly IExportService _exportService = exportService;

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
    public async Task<IActionResult> GetById([FromRoute] int id)
    {
        var visitor = await _visitorRepository.GetByIdAsync(id);
        if(visitor == null)
        {
            return NotFound();
        }
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

        return Ok(created.ToVisitorDto());
    }

    [HttpGet("export")]
    public async Task<IActionResult> ExportVisitors([FromQuery] VisitorExportRequest request)
    {
        var visitorsForExport = await _visitorRepository.GetVisitorsForExportAsync(request);
        if (!visitorsForExport.Any())
            return NotFound("No visitors found for the specified export criteria.");

        byte[] fileContent;
        string fileName;

        if (request.Format == ExportType.Csv)
        {
            visitorsForExport.ForEach(v => v.PhoneNumber = $"=\"{v.PhoneNumber}\"");
            fileContent = _exportService.ExportToCsv(visitorsForExport);
            fileName = $"Visitor_Export_{DateTime.UtcNow:yyyyMMddHHmmss}.csv";

            return File(fileContent, "text/csv", fileName);
        }

        if (request.Format == ExportType.Excel)
        {
            fileContent = _exportService.ExportToExcel(visitorsForExport, "Visitors");
            fileName = $"Visitor_Export_{DateTime.UtcNow:yyyyMMddHHmmss}.xlsx";

            return File(
                fileContent,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileName
            );
        }

        return BadRequest("Invalid export format.");
    }
}
