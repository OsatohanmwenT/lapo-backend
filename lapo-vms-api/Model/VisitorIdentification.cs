namespace lapo_vms_api.Model;

public class VisitorIdentification
{
    public int Id { get; set; }
    public Visitor Visitor { get; set; } = null!;
    public int VisitorId { get; set; }
    public string IdentificationType { get; set; } = string.Empty;
    public string IdentificationNumber { get; set; } = string.Empty;
}
