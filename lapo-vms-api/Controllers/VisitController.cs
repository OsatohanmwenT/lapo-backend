using lapo_vms_api.Dtos;
using lapo_vms_api.Dtos.Visit;
using lapo_vms_api.Helpers;
using lapo_vms_api.Interface;
using lapo_vms_api.Mappers;
using lapo_vms_api.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace lapo_vms_api.Controllers
{
    [Route("api/visits")]
    [ApiController]
    public class VisitController(
        IVisitRepository visitRepository,
        IExportService exportService,
        IUserRepository userRepository,
        IVisitorRepository visitorRepository,
        IAuditService auditService,
        ILogger<VisitController> logger) : ControllerBase
    {
        private readonly IVisitRepository _visitRepository = visitRepository;
        private readonly IVisitorRepository _visitorRepository = visitorRepository;
        private readonly IExportService _exportService = exportService;
        private readonly IUserRepository _userRepository = userRepository;
        private readonly IAuditService _auditService = auditService;
        private readonly ILogger<VisitController> _logger = logger;

        private Guid? GetActorId()
        {
            var actorIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(actorIdClaim, out var actorId) ? actorId : null;
        }

        private string? GetActorRole()
        {
            return User.FindFirstValue(ClaimTypes.Role);
        }

        private string? GetActorStaffId()
        {
            return User.FindFirstValue("staffId");
        }

        private string? GetActorName()
        {
            return User.FindFirstValue(ClaimTypes.Name);
        }

        private Task LogVisitAuditAsync(string eventType, Visit visit, string metadata)
        {
            return _auditService.LogEventAsync(new AuditLog
            {
                EventType = eventType,
                ActorId = GetActorId(),
                ActorRole = GetActorRole(),
                VisitorId = visit.VisitorId,
                Timestamp = WatClock.Now,
                Metadata = $"VisitId: {visit.Id}; {metadata}"
            });
        }

        private void LogVisitFailure(string operation, string reason, Guid? visitId = null)
        {
            _logger.LogWarning(
                "Visit operation failed. Operation={Operation} Reason={Reason} VisitId={VisitId} ActorId={ActorId} StaffId={StaffId} Role={Role}",
                operation,
                reason,
                visitId,
                GetActorId(),
                GetActorStaffId(),
                GetActorRole());
        }

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

        [HttpGet("guest-search")]
        [AllowAnonymous]
        public async Task<IActionResult> GuestSearch([FromQuery] string search)
        {
            if (string.IsNullOrWhiteSpace(search))
                return BadRequest("Search value is required.");

            var visits = await _visitRepository.GuestSearchAsync(search.Trim());
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
        public async Task<IActionResult> GetVisitById(Guid id)
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
        [AllowAnonymous]
        public async Task<IActionResult> CreateVisit([FromForm] CreateVisitDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var normalizedPhone = dto.Visitor.PhoneNumber
                .Replace(" ", "").Replace("+", "").Replace("-", "")
                .Replace("(", "").Replace(")", "");

            // var phoneExists = await _visitorRepository.IsVisitorPhoneExistsAsync(normalizedPhone);
            // if (phoneExists)
            // {
            //     LogVisitFailure("Create", "PhoneAlreadyExists");
            //     return Conflict(new { message = "A visitor with this phone number already exists. Please use a different phone number." });
            // }

            var hasActiveVisit = await _visitRepository.HasActiveVisitByPhoneAsync(normalizedPhone);
            if (hasActiveVisit)
            {
                LogVisitFailure("Create", "ActiveVisitExists");
                return Conflict(new { message = "This visitor already has an active or pending visit. Please wait for it to be completed before registering again." });
            }

            var photoPath = await ImageUploader.UploadImage(dto.Visitor.Photo);

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
                CheckedInBy = string.Empty,
                CheckedOutBy = string.Empty,
                Status = VisitStatus.Pending,
                VisitItems = visitItems
            };

            var createdVisit = await _visitRepository.CreateAsync(visit);

            await LogVisitAuditAsync(
                "VISIT_CREATED",
                createdVisit,
                $"Status: {createdVisit.Status}; HostName: {createdVisit.HostName}; HostDepartment: {createdVisit.HostDepartment}");

            _logger.LogInformation(
                "Visit created. VisitId={VisitId} VisitorId={VisitorId} Status={Status} ActorId={ActorId} StaffId={StaffId} Role={Role}",
                createdVisit.Id,
                createdVisit.VisitorId,
                createdVisit.Status,
                GetActorId(),
                GetActorStaffId(),
                GetActorRole());

            return CreatedAtAction(
                nameof(GetVisitById),
                new { id = createdVisit.Id },
                createdVisit.ToVisitDto());
        }

        [HttpPost("returning")]
        [AllowAnonymous]
        public async Task<IActionResult> CreateReturningVisit([FromBody] CreateReturningVisitDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            if (dto.VisitorId == Guid.Empty || string.IsNullOrWhiteSpace(dto.PurposeOfVisit) || string.IsNullOrWhiteSpace(dto.FloorNumber))
                return BadRequest(new { message = "Visitor, purpose of visit, and floor number are required." });

            var visitor = await _visitorRepository.GetByIdAsync(dto.VisitorId);
            if (visitor == null) return NotFound(new { message = "Visitor not found." });

            if (!Enum.IsDefined(dto.VisitorType))
                return BadRequest(new { message = "Invalid visitor category." });

            var previousVisitorType = visitor.VisitorType;

            var normalizedPhone = visitor.PhoneNumber
                .Replace(" ", "").Replace("+", "").Replace("-", "")
                .Replace("(", "").Replace(")", "");

            if (await _visitRepository.HasActiveVisitByPhoneAsync(normalizedPhone))
            {
                LogVisitFailure("CreateReturning", "ActiveVisitExists");
                return Conflict(new { message = "This visitor already has an active or pending visit. Please wait for it to be completed before registering again." });
            }

            var visit = new Visit
            {
                VisitorId = visitor.Id,
                Visitor = visitor,
                PurposeOfVisit = dto.PurposeOfVisit.Trim(),
                FloorNumber = dto.FloorNumber.Trim(),
                HostName = dto.HostName?.Trim(),
                HostDepartment = dto.HostDepartment?.Trim(),
                CheckedInBy = string.Empty,
                CheckedOutBy = string.Empty,
                Status = VisitStatus.Pending,
                VisitItems = dto.VisitItems
                    .Where(item => item != null)
                    .Select(item => new VisitItem
                    {
                        LaptopModel = item!.LaptopModel,
                        SerialNumber = item.SerialNumber
                    })
                    .ToList()
            };

            visitor.VisitorType = dto.VisitorType;

            var createdVisit = await _visitRepository.CreateAsync(visit);

            await LogVisitAuditAsync(
                "VISIT_CREATED",
                createdVisit,
                $"Status: {createdVisit.Status}; PreviousVisitorType: {previousVisitorType}; VisitorType: {dto.VisitorType}; HostName: {createdVisit.HostName}; HostDepartment: {createdVisit.HostDepartment}");

            return CreatedAtAction(
                nameof(GetVisitById),
                new { id = createdVisit.Id },
                createdVisit.ToVisitDto());
        }

        /// <summary>
        /// Checks out an existing visit and records whether the action was performed by the guest or staff.
        /// </summary>
        /// <param name="id">The unique ID of the visit to check out.</param>
        /// <param name="dto">The payload containing the actor type and actor value for checkout.</param>
        /// <returns>
        /// The updated visit record when checkout is successful; otherwise a bad request or not found response.
        /// </returns>
        [HttpPatch("{id}/checkout")]
        [AllowAnonymous]
        public async Task<IActionResult> CheckOutVisit(Guid id, [FromBody] CheckOutVisitDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var visit = await _visitRepository.GetByIdAsync(id);
            if (visit == null)
            {
                LogVisitFailure("CheckOut", "VisitNotFound", id);
                return NotFound(new { message = "Visit not found." });
            }

            if (visit.Status != VisitStatus.CheckedIn)
            {
                LogVisitFailure("CheckOut", "InvalidVisitStatus", id);
                return BadRequest(new { message = "Only checked-in visits can be checked out." });
            }

            if (string.IsNullOrWhiteSpace(dto.Value))
                return BadRequest(new { message = "Checkout actor value is required." });

            string checkedOutBy;

            if (dto.ActorType == CheckOutActorType.Staff)
            {
                if (User.Identity?.IsAuthenticated != true)
                    return Unauthorized(new { message = "Authentication required for staff checkout." });

                var user = await _userRepository.GetByStaffIdAsync(dto.Value.Trim());
                if (user == null)
                {
                    var actorStaffId = GetActorStaffId();
                    if (string.IsNullOrWhiteSpace(actorStaffId) ||
                        !actorStaffId.Equals(dto.Value.Trim(), StringComparison.OrdinalIgnoreCase))
                    {
                        LogVisitFailure("CheckOut", "StaffUserNotFound", id);
                        return NotFound(new { message = "User not found." });
                    }

                    checkedOutBy = GetActorName() ?? actorStaffId;
                }
                else
                {
                    checkedOutBy = user.Name ?? string.Empty;
                }
            }
            else
            {
                checkedOutBy = dto.Value.Trim();
            }

            var updatedVisit = await _visitRepository.CheckOutAsync(id, WatClock.Now, checkedOutBy);
            if (updatedVisit == null)
            {
                LogVisitFailure("CheckOut", "StatusChangedBeforeUpdate", id);
                return BadRequest("Only checked-in visits can be checked out.");
            }

            await LogVisitAuditAsync(
                "VISIT_CHECKED_OUT",
                updatedVisit,
                $"CheckedOutBy: {checkedOutBy}; ActorType: {dto.ActorType}");

            _logger.LogInformation(
                "Visit checked out. VisitId={VisitId} VisitorId={VisitorId} ActorType={ActorType} ActorId={ActorId} StaffId={StaffId} Role={Role}",
                updatedVisit.Id,
                updatedVisit.VisitorId,
                dto.ActorType,
                GetActorId(),
                GetActorStaffId(),
                GetActorRole());

            return Ok(updatedVisit.ToVisitDto());
        }

        /// <summary>
        /// Reschedules an existing visit to a new date and marks the visit status as rescheduled.
        /// </summary>
        /// <param name="id">The unique ID of the visit to reschedule.</param>
        /// <param name="dto">The payload containing the new reschedule date and time.</param>
        /// <returns>
        /// The updated visit record when the visit exists; otherwise a not found response.
        /// </returns>
        [HttpPatch("{id}/reschedule")]
        public async Task<IActionResult> RescheduleVisit(Guid id, [FromBody] RescheduleVisitDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var visit = await _visitRepository.RescheduleAsync(id, dto.RescheduleDate);
            if (visit == null)
            {
                LogVisitFailure("Reschedule", "VisitNotFound", id);
                return NotFound();
            }

            await LogVisitAuditAsync(
                "VISIT_RESCHEDULED",
                visit,
                $"RescheduleDate: {dto.RescheduleDate:O}");

            _logger.LogInformation(
                "Visit rescheduled. VisitId={VisitId} VisitorId={VisitorId} ActorId={ActorId} StaffId={StaffId} Role={Role}",
                visit.Id,
                visit.VisitorId,
                GetActorId(),
                GetActorStaffId(),
                GetActorRole());

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
        public async Task<IActionResult> CheckInVisit(Guid id)
        {
            var visit = await _visitRepository.GetByIdAsync(id);
            if (visit == null)
            {
                LogVisitFailure("CheckIn", "VisitNotFound", id);
                return NotFound("Visit not found.");
            }

            if (visit.Status != VisitStatus.Pending && visit.Status != VisitStatus.Rescheduled)
            {
                LogVisitFailure("CheckIn", "InvalidVisitStatus", id);
                return BadRequest("Only pending or rescheduled visits can be checked in.");
            }

            var checkedInBy = User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? User.FindFirstValue("staffId")
                ?? User.FindFirstValue(ClaimTypes.Name);

            if (string.IsNullOrWhiteSpace(checkedInBy))
            {
                LogVisitFailure("CheckIn", "ActorIdentityMissing", id);
                return Unauthorized("User identity could not be resolved.");
            }

            var updatedVisit = await _visitRepository.CheckInAsync(id, WatClock.Now, checkedInBy);
            if (updatedVisit == null)
            {
                LogVisitFailure("CheckIn", "StatusChangedBeforeUpdate", id);
                return BadRequest("Only pending or rescheduled visits can be checked in.");
            }

            await LogVisitAuditAsync(
                "VISIT_CHECKED_IN",
                updatedVisit,
                $"CheckedInBy: {checkedInBy}");

            _logger.LogInformation(
                "Visit checked in. VisitId={VisitId} VisitorId={VisitorId} ActorId={ActorId} StaffId={StaffId} Role={Role}",
                updatedVisit.Id,
                updatedVisit.VisitorId,
                GetActorId(),
                GetActorStaffId(),
                GetActorRole());

            return Ok(updatedVisit.ToVisitDto());
        }

        /// <summary>
        /// Updates the tag number for an existing visit.
        /// </summary>
        /// <param name="id">The unique ID of the visit to update.</param>
        /// <param name="dto">The payload containing the new tag number value.</param>
        /// <returns>
        /// The updated visit record when the visit exists and the payload is valid;
        /// otherwise a bad request or not found response.
        /// </returns>
        [HttpPatch("{id}/tag-number")]
        public async Task<IActionResult> UpdateVisitTagNumber(Guid id, [FromBody] UpdateVisitTagNumberDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            if (string.IsNullOrWhiteSpace(dto.TagNumber))
                return BadRequest("Tag number cannot be empty.");

            var tagInUse = await _visitRepository.IsTagNumberInUseAsync(dto.TagNumber.Trim(), id);
            if (tagInUse)
            {
                LogVisitFailure("UpdateTag", "TagAlreadyInUse", id);
                return Conflict(new { message = $"Tag number '{dto.TagNumber.Trim()}' is already assigned to another checked-in visitor." });
            }

            var visit = await _visitRepository.UpdateTagNumberAsync(id, dto.TagNumber.Trim());
            if (visit == null)
            {
                LogVisitFailure("UpdateTag", "VisitNotFound", id);
                return NotFound();
            }

            await LogVisitAuditAsync(
                "VISIT_TAG_UPDATED",
                visit,
                $"TagNumber: {visit.TagNumber}");

            _logger.LogInformation(
                "Visit tag updated. VisitId={VisitId} VisitorId={VisitorId} ActorId={ActorId} StaffId={StaffId} Role={Role}",
                visit.Id,
                visit.VisitorId,
                GetActorId(),
                GetActorStaffId(),
                GetActorRole());

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
        public async Task<IActionResult> RejectVisit(Guid id)
        {
            var visit = await _visitRepository.GetByIdAsync(id);
            if (visit == null)
            {
                LogVisitFailure("Reject", "VisitNotFound", id);
                return NotFound();
            }

            if (visit.Status != VisitStatus.Pending && visit.Status != VisitStatus.Rescheduled)
            {
                LogVisitFailure("Reject", "InvalidVisitStatus", id);
                return BadRequest("Only pending or rescheduled visits can be rejected.");
            }

            var previousStatus = visit.Status;
            var updatedVisit = await _visitRepository.UpdateStatusAsync(id, VisitStatus.Rejected);
            if (updatedVisit == null)
            {
                LogVisitFailure("Reject", "UpdateFailed", id);
                return BadRequest("Visit status could not be updated.");
            }

            await LogVisitAuditAsync(
                "VISIT_REJECTED",
                updatedVisit,
                $"PreviousStatus: {previousStatus}; NewStatus: {VisitStatus.Rejected}");

            _logger.LogInformation(
                "Visit rejected. VisitId={VisitId} VisitorId={VisitorId} PreviousStatus={PreviousStatus} ActorId={ActorId} StaffId={StaffId} Role={Role}",
                updatedVisit.Id,
                updatedVisit.VisitorId,
                previousStatus,
                GetActorId(),
                GetActorStaffId(),
                GetActorRole());

            return Ok(updatedVisit.ToVisitDto());
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
        public async Task<IActionResult> UpdateVisitStatus(Guid id, [FromBody] VisitStatus status)
        {
            var visit = await _visitRepository.GetByIdAsync(id);
            if (visit == null)
            {
                LogVisitFailure("UpdateStatus", "VisitNotFound", id);
                return NotFound();
            }

            var previousStatus = visit.Status;
            var updatedVisit = await _visitRepository.UpdateStatusAsync(id, status);
            if (updatedVisit == null)
            {
                LogVisitFailure("UpdateStatus", "UpdateFailed", id);
                return BadRequest("Visit status could not be updated.");
            }

            await LogVisitAuditAsync(
                "VISIT_STATUS_UPDATED",
                updatedVisit,
                $"PreviousStatus: {previousStatus}; NewStatus: {status}");

            _logger.LogInformation(
                "Visit status updated. VisitId={VisitId} VisitorId={VisitorId} PreviousStatus={PreviousStatus} NewStatus={NewStatus} ActorId={ActorId} StaffId={StaffId} Role={Role}",
                updatedVisit.Id,
                updatedVisit.VisitorId,
                previousStatus,
                status,
                GetActorId(),
                GetActorStaffId(),
                GetActorRole());

            return Ok(updatedVisit.ToVisitDto());
        }

        [HttpGet("export")]
        public async Task<IActionResult> ExportVisits([FromQuery] VisitExportRequest request)
        {
            var visitsForExport = await _visitRepository.GetVisitsForExportAsync(request);
            if (visitsForExport == null || !visitsForExport.Any())
            {
                LogVisitFailure("Export", "NoRecordsFound");
                return NotFound("No visits found for the specified export criteria.");
            }


            byte[] fileContent;
            string fileName;

            if (request.Format == ExportType.Csv)
            {
                visitsForExport.ForEach(v => v.VisitorPhoneNumber = $"=\"{v.VisitorPhoneNumber}\"");
                fileContent = _exportService.ExportToCsv(visitsForExport);
                fileName = $"Visit_Export_{DateTime.UtcNow:yyyyMMddHHmmss}.csv";

                _logger.LogInformation(
                    "Visits exported. Format={Format} RecordCount={RecordCount} ActorId={ActorId} StaffId={StaffId} Role={Role}",
                    request.Format,
                    visitsForExport.Count,
                    GetActorId(),
                    GetActorStaffId(),
                    GetActorRole());

                return File(fileContent, "text/csv", fileName);
            }

            if (request.Format == ExportType.Excel)
            {
                fileContent = _exportService.ExportToExcel(visitsForExport, "Visits");
                fileName = $"Visit_Export_{DateTime.UtcNow:yyyyMMddHHmmss}.xlsx";

                _logger.LogInformation(
                    "Visits exported. Format={Format} RecordCount={RecordCount} ActorId={ActorId} StaffId={StaffId} Role={Role}",
                    request.Format,
                    visitsForExport.Count,
                    GetActorId(),
                    GetActorStaffId(),
                    GetActorRole());

                return File(
                    fileContent,
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    fileName
                );
            }

            LogVisitFailure("Export", "InvalidFormat");
            return BadRequest("Invalid export format.");

        }

    }
}
