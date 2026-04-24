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

        /// <summary>
        /// Retrieves all visit records and applies any supplied query filters,
        /// paging, or search options before returning the result.
        /// </summary>
        /// <param name="queryParameters">
        /// Query string options used to filter, search, sort, or paginate the visit list.
        /// </param>
        /// <returns>
        /// A collection of visit records formatted as visit DTOs.
        /// </returns>
        [HttpGet]
        public async Task<IActionResult> GetAllVisits([FromQuery] QueryParameters queryParameters)
        {
            var visits = await _visitRepository.GetAllAsync(queryParameters);
            return Ok(visits.Select(v => v.ToVisitDto()));
        }

        /// <summary>
        /// Retrieves the complete details of a single visit using the visit's unique identifier.
        /// </summary>
        /// <param name="id">The unique ID of the visit to retrieve.</param>
        /// <returns>
        /// The matching visit record when found; otherwise a not found response.
        /// </returns>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetVisitById(int id)
        {
            var visit = await _visitRepository.GetByIdAsync(id);
            if (visit == null) return NotFound();

            return Ok(visit.ToVisitDto());
        }

        /// <summary>
        /// Creates a new visit record together with the nested visitor information,
        /// optional identification details, optional worker details, and any submitted visit items.
        /// </summary>
        /// <param name="dto">
        /// The multipart form payload containing the visitor details, visit metadata,
        /// and optional items being brought in for the visit.
        /// </param>
        /// <returns>
        /// The created visit record with a location header pointing to the visit lookup endpoint.
        /// </returns>
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
                PurposeOfVisit = dto.PurposeOfVisit ?? string.Empty,
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

        /// <summary>
        /// Checks out an existing visit by setting the checkout time to the current UTC time
        /// and updating the visit status to checked out.
        /// </summary>
        /// <param name="id">The unique ID of the visit to check out.</param>
        /// <returns>
        /// The updated visit record when checkout is successful; otherwise a bad request or not found response.
        /// </returns>
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

        /// <summary>
        /// Reschedules an existing visit to a new date and marks the visit status as rescheduled.
        /// </summary>
        /// <param name="id">The unique ID of the visit to reschedule.</param>
        /// <param name="newDate">The new date and time that should be stored for the visit reschedule.</param>
        /// <returns>
        /// The updated visit record when the visit exists; otherwise a not found response.
        /// </returns>
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

        /// <summary>
        /// Checks in a pending visit by setting the check-in time to the current UTC time
        /// and updating the visit status to checked in.
        /// </summary>
        /// <param name="id">The unique ID of the visit to check in.</param>
        /// <returns>
        /// The updated visit record when check-in is successful; otherwise a bad request or not found response.
        /// </returns>
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

        /// <summary>
        /// Checks in a pending visit by setting the status to rejected.
        /// </summary>
        /// <param name="id">The unique ID of the visit to reject.</param>
        /// <returns>
        /// The updated visit record when reject is successful; otherwise a bad request or not found response.
        /// </returns>
        [HttpPatch("{id}/reject")]
        public async Task<IActionResult> RejectVisit(int id)
        {
            var visit = await _visitRepository.GetByIdAsync(id);
            if (visit == null) return NotFound();

            if (visit.Status != VisitStatus.Pending)
                return BadRequest("Only pending visits can be rejected.");

            visit.CheckInTime = DateTime.UtcNow;
            visit.Status = VisitStatus.Rejected;

            await _visitRepository.UpdateStatusAsync(id, VisitStatus.Rejected);

            return Ok(visit.ToVisitDto());
        }

        /// <summary>
        /// Updates the status of an existing visit to the supplied status value
        /// without modifying any other visit details.
        /// </summary>
        /// <param name="id">The unique ID of the visit to update.</param>
        /// <param name="status">The new visit status to persist.</param>
        /// <returns>
        /// The updated visit record when the visit exists; otherwise a not found response.
        /// </returns>
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
