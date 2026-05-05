using System;

namespace lapo_vms_api.Model;

public class AuditLog
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string EventType { get; set; } = string.Empty;
    public Guid? ActorId { get; set; }
    public string? ActorRole { get; set; }
    public Guid? VisitorId { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string? Metadata { get; set; }

    public string? PrevHash { get; set; }
    public string CurrentHash { get; set; } = string.Empty;
}
