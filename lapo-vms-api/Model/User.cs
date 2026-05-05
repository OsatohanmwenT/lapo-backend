using System;

namespace lapo_vms_api.Model;

public class User
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string? Name { get; set; }
    public string? Email { get; set; }
    public string? StaffId { get; set; }
    public UserRole? Role { get; set; } = UserRole.Admin;
    public DateTime CreatedAt { get; set; }
}
