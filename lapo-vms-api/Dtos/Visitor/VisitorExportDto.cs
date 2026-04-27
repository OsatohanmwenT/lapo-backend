namespace lapo_vms_api.Dtos.Visitor;

public class VisitorExportDto
{
    public string FullName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string VisitorType { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public string IdentificationType { get; set; } = string.Empty;
    public string IdentificationNumber { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
