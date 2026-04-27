using System.ComponentModel.DataAnnotations;

namespace lapo_vms_api.Dtos.Visit;

public class CheckOutVisitDto
{
    [Required]
    public CheckOutActorType ActorType { get; set; }

    [Required]
    public string Value { get; set; } = string.Empty;
}

public enum CheckOutActorType
{
    Guest,
    Staff
}
