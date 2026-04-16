using System;
using lapo_vms_api.Dtos.Visit;
using lapo_vms_api.Dtos.VisitorIdentification;
using lapo_vms_api.Dtos.WorkerDetails;

namespace lapo_vms_api.Dtos.Visitor;

public class VisitorDto
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string PhotoPath { get; set; } = string.Empty;
    public string VisitorType { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public VisitorIdentificationDto? Identification { get; set; }
    public WorkerDetailsDto? WorkerDetails { get; set; }
    public List<VisitDto> Visits { get; set; } = new();
}
