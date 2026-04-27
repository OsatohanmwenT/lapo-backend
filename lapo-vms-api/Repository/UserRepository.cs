using lapo_vms_api.Data;
using lapo_vms_api.Interface;
using lapo_vms_api.Model;
using Microsoft.EntityFrameworkCore;

namespace lapo_vms_api.Repository;

public class UserRepository(ApplicationDBContext context) : IUserRepository
{
    private readonly ApplicationDBContext _context = context;

    public async Task<User> CreateAsync(User userModel)
    {
        userModel.CreatedAt = DateTime.UtcNow;

        await _context.User.AddAsync(userModel);
        await _context.SaveChangesAsync();
        return userModel;
    }

    public async Task<User?> UpdateAsync(int id, User userModel)
    {
        var existing = await _context.User.FindAsync(id);
        if (existing == null) return null;

        existing.Name = userModel.Name;
        existing.Email = userModel.Email;
        existing.StaffId = userModel.StaffId;
        existing.Role = userModel.Role;

        await _context.SaveChangesAsync();
        return existing;
    }

    public async Task<User?> DeleteAsync(int id)
    {
        var user = await _context.User.FindAsync(id);
        if (user == null) return null;

        _context.User.Remove(user);
        await _context.SaveChangesAsync();
        return user;
    }

    public async Task<bool> ExistsByEmailAsync(string email, int? excludeId = null)
    {
        return await _context.User.AnyAsync(u =>
            u.Email == email &&
            (!excludeId.HasValue || u.Id != excludeId.Value));
    }

    public async Task<bool> ExistsByStaffIdAsync(string staffId, int? excludeId = null)
    {
        return await _context.User.AnyAsync(u =>
            u.StaffId == staffId &&
            (!excludeId.HasValue || u.Id != excludeId.Value));
    }
}
