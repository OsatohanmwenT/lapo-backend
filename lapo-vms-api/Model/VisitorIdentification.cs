namespace lapo_vms_api.Model;

public class VisitorIdentification
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Visitor Visitor { get; set; } = null!;
    public Guid VisitorId { get; set; }
    public string IdentificationType { get; set; } = string.Empty;
    public string IdentificationNumber { get; set; } = string.Empty;
}
