using System;

namespace lapo_vms_api.Dtos.VisitItem;

public class VisitItemDto
{
    public Guid Id { get; set; }
    public string? SerialNumber { get; set; }
    public string? LaptopModel { get; set; }
    public Guid VisitId { get; set; }
}
