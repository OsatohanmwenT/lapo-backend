namespace lapo_vms_api.Dtos.Visitor;

public class VisitorExportDto
{
    public string FullName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string VisitorType { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public string IdentificationType { get; set; } = string.Empty;
    public string IdentificationNumber { get; set; } = string.Empty;
    public string HostName { get; set; } = string.Empty;
    public string HostDepartment { get; set; } = string.Empty;
    public string? TagNumber { get; set; }
    public string PurposeOfVisit { get; set; } = string.Empty;
    public string FloorNumber { get; set; } = string.Empty;
    public DateTime? CheckInTime { get; set; }
    public DateTime? CheckOutTime { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime RegisteredAt { get; set; }
}
