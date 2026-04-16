using System.ComponentModel.DataAnnotations;
using lapo_vms_api.Dtos.VisitorIdentification;
using lapo_vms_api.Model;

namespace lapo_vms_api.Dtos.Visitor;

public class CreateVisitorDto
{
    [Required]
    [StringLength(100)]
    public string FullName { get; set; } = string.Empty;

    [Required]
    [Phone]
    public string PhoneNumber { get; set; } = string.Empty;

    [Required]
    public VisitorType VisitorType { get; set; } = VisitorType.Customer;

    public CreateVisitorIdentificationDto? Identification { get; set; }

    public string? CompanyName { get; set; }

    [Required]
    public IFormFile Photo { get; set; } = null!;
}
