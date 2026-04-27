using lapo_vms_api.Helpers;
using lapo_vms_api.Model;

namespace lapo_vms_api.Interface;

public interface IUserRepository
{
    Task<List<User>> GetAllAsync(QueryParameters queryParameters);
    Task<User?> GetByIdAsync(int id);
    Task<User?> GetByStaffIdAsync(string staffId);
    Task<User> CreateAsync(User userModel);
    Task<User?> UpdateAsync(int id, User userModel);
    Task<User?> DeleteAsync(int id);
    Task<bool> ExistsByEmailAsync(string email, int? excludeId = null);
    Task<bool> ExistsByStaffIdAsync(string staffId, int? excludeId = null);
}
