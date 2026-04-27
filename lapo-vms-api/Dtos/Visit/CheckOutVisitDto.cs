using System.ComponentModel.DataAnnotations;

namespace lapo_vms_api.Dtos.Visit;

public class CheckOutVisitDto
{
    [Range(1, int.MaxValue)]
    public required string StaffId { get; set; }
}
