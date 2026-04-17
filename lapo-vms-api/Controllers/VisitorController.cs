using lapo_vms_api.Dtos.Visitor;
using lapo_vms_api.Helpers;
using lapo_vms_api.Interface;
using lapo_vms_api.Mappers;
using Microsoft.AspNetCore.Mvc;

namespace lapo_vms_api.Controllers;

[Route("api/visitors")]
[ApiController]
public class VisitorController(IVisitorRepository visitorRepository) : ControllerBase
{
    private readonly IVisitorRepository _visitorRepository = visitorRepository;

    [HttpGet]
    public async Task<IActionResult> GetAllVisitors([FromQuery] QueryObject query)
    {
        var visitors = await _visitorRepository.GetAllAsync(query);
        return Ok(visitors.Select(v => v.ToVisitorDto()));
    }

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
}
