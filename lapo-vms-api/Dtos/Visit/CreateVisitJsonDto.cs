using System.ComponentModel.DataAnnotations;
using lapo_vms_api.Dtos.VisitItem;
using lapo_vms_api.Dtos.Visitor;

namespace lapo_vms_api.Dtos.Visit;

public class CreateVisitJsonDto
{
    [Required]
    public CreateVisitorJsonDto Visitor { get; set; } = null!;

    [Required]
    [StringLength(500)]
    public string PurposeOfVisit { get; set; } = string.Empty;

    [Required]
    public string FloorNumber { get; set; } = string.Empty;

    public string? HostName { get; set; }

    public string? HostDepartment { get; set; }

    public ICollection<CreateVisitItemDto?> VisitItems { get; set; } = new List<CreateVisitItemDto?>();
}
