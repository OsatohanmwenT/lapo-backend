using System;

namespace lapo_vms_api.Dtos.Visit;

public class ExportVisitsDto
{
    public string VisitorName { get; set; } = string.Empty;
    public string VisitorPhoneNumber { get; set; } = string.Empty;
    public string PurposeOfVisit { get; set; } = string.Empty;
    public string TagNumber { get; set; } = string.Empty;
    public string FloorNumber { get; set; } = string.Empty;
    public string HostName { get; set; } = string.Empty;
    public string HostDepartment { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public string IdentificationType { get; set; } = string.Empty;
    public string IdentificationNumber { get; set; } = string.Empty;
    public DateTime CheckInTime { get; set; }
    public DateTime? CheckOutTime { get; set; }
    public DateTime RegisteredAt { get; set; }
    public string Status { get; set; } = string.Empty;
}
