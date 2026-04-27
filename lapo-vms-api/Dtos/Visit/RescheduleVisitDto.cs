using System.ComponentModel.DataAnnotations;

namespace lapo_vms_api.Dtos.Visit;

public class RescheduleVisitDto
{
    [Range(typeof(DateTime), "0001-01-02", "9999-12-31")]
    public DateTime RescheduleDate { get; set; }
}
