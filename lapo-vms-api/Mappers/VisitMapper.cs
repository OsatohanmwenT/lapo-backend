using lapo_vms_api.Dtos.Visit;
using lapo_vms_api.Model;

namespace lapo_vms_api.Mappers;

public static class VisitMapper
{
    public static VisitDto ToVisitDto(this Visit visit)
    {
        return new VisitDto
        {
            Id = visit.Id,
            VisitorId = visit.VisitorId,
            PurposeOfVisit = visit.PurposeOfVisit,
            TagNumber = visit.TagNumber,
            FloorNumber = visit.FloorNumber,
            HostName = visit.HostName,
            HostDepartment = visit.HostDepartment,
            RescheduleDate = visit.RescheduleDate,
            RegisteredAt = visit.RegisteredAt,
            CheckInTime = visit.CheckInTime,
            CheckOutTime = visit.CheckOutTime,
            CheckedOutBy = visit.CheckedOutBy,
            Status = visit.Status,
            VisitItems = visit.VisitItems.Select(item => item.ToVisitItemDto()).ToList()
        };
    }

    public static Visit ToVisitFromCreateDto(this CreateVisitDto dto)
    {
        return new Visit
        {
            Visitor = dto.Visitor.ToVisitorFromCreateDto(),
            PurposeOfVisit = dto.PurposeOfVisit,
            FloorNumber = dto.FloorNumber,
            HostName = dto.HostName,
            HostDepartment = dto.HostDepartment,
            RegisteredAt = DateTime.UtcNow,
            CheckInTime = DateTime.UtcNow,
            Status = VisitStatus.Pending,
            CheckedOutBy = string.Empty
        };
    }
}
