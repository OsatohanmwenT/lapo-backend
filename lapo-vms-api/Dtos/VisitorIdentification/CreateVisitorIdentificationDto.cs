using System.ComponentModel.DataAnnotations;

namespace lapo_vms_api.Dtos.VisitorIdentification;

public class CreateVisitorIdentificationDto
{
    [StringLength(100)]
    public string IdentificationType { get; set; } = string.Empty;

    [StringLength(100)]
    public string IdentificationNumber { get; set; } = string.Empty;
}
