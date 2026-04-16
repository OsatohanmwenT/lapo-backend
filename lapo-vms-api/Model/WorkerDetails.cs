namespace lapo_vms_api.Model;

public class WorkerDetails
{
    public int Id { get; set; }

    public Visitor Visitor { get; set; } = null!;

    public int VisitorId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
}
