using System.ComponentModel.DataAnnotations;

namespace lapo_vms_api.Dtos.Visit;

public class UpdateVisitTagNumberDto
{
    [Required]
    [StringLength(50)]
    public string TagNumber { get; set; } = string.Empty;
}