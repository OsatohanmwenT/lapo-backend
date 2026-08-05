using System.ComponentModel.DataAnnotations;
using lapo_vms_api.Dtos.VisitItem;

namespace lapo_vms_api.Dtos.Visit;

public class CreateReturningVisitDto
{
    [Required]
    public Guid VisitorId { get; set; }

    [Required]
    public string PurposeOfVisit { get; set; } = string.Empty;

    [Required]
    public string FloorNumber { get; set; } = string.Empty;

    public string? HostName { get; set; }
    public string? HostDepartment { get; set; }
    public ICollection<CreateVisitItemDto?> VisitItems { get; set; } = new List<CreateVisitItemDto?>();
}
