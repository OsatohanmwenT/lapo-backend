using lapo_vms_api.Dtos.VisitorIdentification;
using lapo_vms_api.Dtos.WorkerDetails;

namespace lapo_vms_api.Dtos.Visit;

public class VisitVisitorDto
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string PhotoPath { get; set; } = string.Empty;
    public string VisitorType { get; set; } = string.Empty;
    public VisitorIdentificationDto? Identification { get; set; }
    public WorkerDetailsDto? WorkerDetails { get; set; }
}
