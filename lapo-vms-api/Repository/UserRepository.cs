using lapo_vms_api.Data;
using lapo_vms_api.Helpers;
using lapo_vms_api.Interface;
using lapo_vms_api.Model;
using Microsoft.EntityFrameworkCore;

namespace lapo_vms_api.Repository;

public class UserRepository(ApplicationDBContext context) : IUserRepository
{
    private readonly ApplicationDBContext _context = context;

    public async Task<List<User>> GetAllAsync(QueryParameters queryParameters)
    {
        var users = _context.User.AsQueryable();

        if (!string.IsNullOrWhiteSpace(queryParameters.Search))
        {
            users = users.Where(u =>
                (u.Name != null && u.Name.Contains(queryParameters.Search)) ||
                (u.Email != null && u.Email.Contains(queryParameters.Search))
            );
        }

        return await users
            .OrderByDescending(u => u.CreatedAt)
            .Skip((queryParameters.PageNumber - 1) * queryParameters.PageSize)
            .Take(queryParameters.PageSize)
            .ToListAsync();
    }

    public async Task<User?> GetByIdAsync(int id)
    {
        return await _context.User.FirstOrDefaultAsync(u => u.Id == id);
    }
    
    public async Task<User?> GetByStaffIdAsync(string staffId)
    {
        return await _context.User.FirstOrDefaultAsync(u => u.StaffId == staffId);
    }

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
