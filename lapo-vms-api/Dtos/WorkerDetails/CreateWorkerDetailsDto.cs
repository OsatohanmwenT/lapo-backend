using System.ComponentModel.DataAnnotations;

namespace lapo_vms_api.Dtos.WorkerDetails;

public class CreateWorkerDetailsDto
{
    [Required]
    [StringLength(150)]
    public string CompanyName { get; set; } = string.Empty;
}