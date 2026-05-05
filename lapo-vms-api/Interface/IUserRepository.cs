using lapo_vms_api.Helpers;
using lapo_vms_api.Model;

namespace lapo_vms_api.Interface;

public interface IUserRepository
{
    Task<List<User>> GetAllAsync(QueryParameters queryParameters);
    Task<User?> GetByIdAsync(Guid id);
    Task<User?> GetByStaffIdAsync(string staffId);
    Task<User> CreateAsync(User userModel);
    Task<User?> UpdateAsync(Guid id, User userModel);
    Task<User?> DeleteAsync(Guid id);
    Task<bool> ExistsByEmailAsync(string email, Guid? excludeId = null);
    Task<bool> ExistsByStaffIdAsync(string staffId, Guid? excludeId = null);
}
