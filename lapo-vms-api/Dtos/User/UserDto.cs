using lapo_vms_api.Model;

namespace lapo_vms_api.Dtos.User;

public class UserDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string StaffId { get; set; } = string.Empty;
    public UserRole? Role { get; set; }
    public DateTime CreatedAt { get; set; }
}
