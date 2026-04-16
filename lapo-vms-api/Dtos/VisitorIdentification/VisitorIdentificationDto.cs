namespace lapo_vms_api.Dtos.VisitorIdentification;

public class VisitorIdentificationDto
{
    public int Id { get; set; }
    public string IdentificationType { get; set; } = string.Empty;
    public string IdentificationNumber { get; set; } = string.Empty;
}
