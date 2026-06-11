using System;

namespace lapo_vms_api.Dtos.Visit;

public class ExportVisitsDto
{
    public Guid VisitorId { get; set; }
    public string VisitorName { get; set; } = string.Empty;
    public string VisitorPhoneNumber { get; set; } = string.Empty;
    public string VisitorType { get; set; } = string.Empty;
    public string PurposeOfVisit { get; set; } = string.Empty;
    public string TagNumber { get; set; } = string.Empty;
    public string FloorNumber { get; set; } = string.Empty;
    public string HostName { get; set; } = string.Empty;
    public string HostDepartment { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public string IdentificationType { get; set; } = string.Empty;
    public string IdentificationNumber { get; set; } = string.Empty;
    public string VisitItems { get; set; } = string.Empty;
    public DateTime? RescheduleDate { get; set; }
    public DateTime? CheckInTime { get; set; }
    public string CheckedInBy { get; set; } = string.Empty;
    public DateTime? CheckOutTime { get; set; }
    public string CheckedOutBy { get; set; } = string.Empty;
    public DateTime RegisteredAt { get; set; }
    public string Status { get; set; } = string.Empty;
}
