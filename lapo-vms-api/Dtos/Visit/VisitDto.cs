using lapo_vms_api.Dtos.VisitItem;
using lapo_vms_api.Model;

namespace lapo_vms_api.Dtos.Visit;

public class VisitDto
{
    public int Id { get; set; }
    public int VisitorId { get; set; }
    public VisitVisitorDto? Visitor { get; set; }
    public string PurposeOfVisit { get; set; } = string.Empty;
    public string? TagNumber { get; set; }
    public string FloorNumber { get; set; } = string.Empty;
    public string? HostName { get; set; }
    public string? HostDepartment { get; set; }
    public DateTime? RescheduleDate { get; set; }
    public DateTime RegisteredAt { get; set; }
    public DateTime? CheckInTime { get; set; }
    public string CheckedInBy { get; set; } = string.Empty;
    public DateTime? CheckOutTime { get; set; }
    public string CheckedOutBy { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public List<VisitItemDto> VisitItems { get; set; } = new();
}
