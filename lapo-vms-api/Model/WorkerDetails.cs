namespace lapo_vms_api.Model;

public class WorkerDetails
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Visitor Visitor { get; set; } = null!;

    public Guid VisitorId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
}
