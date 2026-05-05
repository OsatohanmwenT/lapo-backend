namespace lapo_vms_api.Dtos.WorkerDetails;

public class WorkerDetailsDto
{
    public Guid Id { get; set; }
    public Guid VisitorId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
}
