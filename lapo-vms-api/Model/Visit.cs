namespace lapo_vms_api.Model
{
    public class Visit
    {
    public Guid Id { get; set; } = Guid.NewGuid();
    public Visitor Visitor { get; set; } = null!;
    public Guid VisitorId { get; set; }
    public string PurposeOfVisit { get; set; } = string.Empty;

    public string? TagNumber { get; set; }
    public string FloorNumber { get; set; } = string.Empty;
    public string? HostName { get; set; }
    public string? HostDepartment { get; set; }

    public DateTime? RescheduleDate { get; set; }
    public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;
    public DateTime? CheckInTime { get; set; }
    public string CheckedInBy { get; set; } = string.Empty;
    public DateTime? CheckOutTime { get; set; }
    public string CheckedOutBy { get; set; } = string.Empty;

    public VisitStatus Status { get; set; } = VisitStatus.Pending;
    public ICollection<VisitItem> VisitItems { get; set; } = new List<VisitItem>();

    }
}
