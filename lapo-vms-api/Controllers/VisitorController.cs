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
    [Consumes("application/json")]
    public async Task<IActionResult> CreateVisitor([FromBody] CreateVisitorJsonDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        try
        {
            var photoPath = await ImageUploader.UploadImage(dto.Photo);

            var visitor = dto.ToVisitorFromCreateJsonDto();
            visitor.PhotoPath = photoPath;

            var created = await _visitorRepository.CreateAsync(visitor);

            return Ok(created.ToVisitorDto());
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
