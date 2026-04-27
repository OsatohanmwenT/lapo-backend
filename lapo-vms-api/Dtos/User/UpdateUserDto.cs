using System.ComponentModel.DataAnnotations;
using lapo_vms_api.Model;

namespace lapo_vms_api.Dtos.User;

public class UpdateUserDto
{
    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [StringLength(50)]
    public string StaffId { get; set; } = string.Empty;

    [Required]
    public UserRole Role { get; set; } = UserRole.Admin;
}
