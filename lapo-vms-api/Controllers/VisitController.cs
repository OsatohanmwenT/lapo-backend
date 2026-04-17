using lapo_vms_api.Dtos.Visit;
using lapo_vms_api.Helpers;
using lapo_vms_api.Interface;
using lapo_vms_api.Mappers;
using lapo_vms_api.Model;
using Microsoft.AspNetCore.Mvc;

namespace lapo_vms_api.Controllers
{
    [Route("api/visits")]
    [ApiController]
    public class VisitController(IVisitRepository visitRepository) : ControllerBase
    {
        private readonly IVisitRepository _visitRepository = visitRepository;

        [HttpGet]
        public async Task<IActionResult> GetAllVisits()
        {
            var visits = await _visitRepository.GetAllAsync();
            return Ok(visits.Select(v => v.ToVisitDto()));
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetVisitById(int id)
        {
            var visit = await _visitRepository.GetByIdAsync(id);
            if (visit == null) return NotFound();

            return Ok(visit.ToVisitDto());
        }

        [HttpPost]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> CreateVisit([FromForm] CreateVisitDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var photoPath = await ImageUploader.UploadImage(dto.Visitor.Photo);
            Console.WriteLine($"Photo Path: {photoPath}");

            var visitor = new Visitor
            {
                FullName = dto.Visitor.FullName,
                PhoneNumber = dto.Visitor.PhoneNumber,
                PhotoPath = photoPath,
                VisitorType = dto.Visitor.VisitorType
            };

            if (dto.Visitor.Identification != null)
            {
                visitor.Identification = new VisitorIdentification
                {
                    IdentificationType = dto.Visitor.Identification.IdentificationType,
                    IdentificationNumber = dto.Visitor.Identification.IdentificationNumber
                };
            }

            if (!string.IsNullOrWhiteSpace(dto.Visitor.CompanyName))
            {
                visitor.WorkerDetails = new WorkerDetails
                {
                    CompanyName = dto.Visitor.CompanyName
                };
            }

            var visitItems = dto.VisitItems
                .Where(vi => vi != null)
                .Select(vi => new VisitItem
                {
                    SerialNumber = vi!.SerialNumber,
                    LaptopModel = vi.LaptopModel
                })
                .ToList();

            var visit = new Visit
            {
                Visitor = visitor,
                PurposeOfVisit = dto.PurposeOfVisit,
                FloorNumber = dto.FloorNumber,
                HostName = dto.HostName,
                HostDepartment = dto.HostDepartment,
                CheckInTime = DateTime.UtcNow,
                CheckedOutBy = string.Empty,
                Status = VisitStatus.Pending,
                VisitItems = visitItems
            };

            var createdVisit = await _visitRepository.CreateAsync(visit);

            return CreatedAtAction(
                nameof(GetVisitById),
                new { id = createdVisit.Id },
                createdVisit.ToVisitDto());
        }

        [HttpPatch("{id}/checkout")]
        public async Task<IActionResult> CheckOutVisit(int id)
        {
            var visit = await _visitRepository.GetByIdAsync(id);
            if (visit == null) return NotFound();

            if (visit.Status == VisitStatus.CheckedOut)
                return BadRequest("Visit is already checked out.");

            visit.CheckOutTime = DateTime.UtcNow;
            // visit.CheckedOutBy = dto.CheckedOutBy;
            visit.Status = VisitStatus.CheckedOut;

            await _visitRepository.UpdateStatusAsync(id, VisitStatus.CheckedOut);

            return Ok(visit.ToVisitDto());
        }

        [HttpPatch("{id}/reschedule")]
        public async Task<IActionResult> RescheduleVisit(int id, [FromBody] DateTime newDate)
        {
            var visit = await _visitRepository.GetByIdAsync(id);
            if (visit == null) return NotFound();

            visit.RescheduleDate = newDate;
            visit.Status = VisitStatus.Rescheduled;

            await _visitRepository.UpdateStatusAsync(id, VisitStatus.Rescheduled);

            return Ok(visit.ToVisitDto());
        }

        [HttpPatch("{id}/check-in")]
        public async Task<IActionResult> CheckInVisit(int id)
        {
            var visit = await _visitRepository.GetByIdAsync(id);
            if (visit == null) return NotFound();

            if (visit.Status != VisitStatus.Pending)
                return BadRequest("Only pending visits can be checked in.");

            visit.CheckInTime = DateTime.UtcNow;
            visit.Status = VisitStatus.CheckedIn;

            await _visitRepository.UpdateStatusAsync(id, VisitStatus.CheckedIn);

            return Ok(visit.ToVisitDto());
        }

        [HttpPatch("{id}/status")]
        public async Task<IActionResult> UpdateVisitStatus(int id, [FromBody] VisitStatus status)
        {
            var visit = await _visitRepository.GetByIdAsync(id);
            if (visit == null) return NotFound();

            visit.Status = status;
            await _visitRepository.UpdateStatusAsync(id, status);

            return Ok(visit.ToVisitDto());
        }

    }
}
