using System;
using lapo_vms_api.Model;

namespace lapo_vms_api.Interface;

public interface IVisitItemRepository
{
    Task<List<VisitItem>> GetAllAsync();
    Task<VisitItem?> GetByIdAsync(Guid id);
    Task<VisitItem> CreateAsync(VisitItem itemModel);
    Task<VisitItem?> UpdateAsync(Guid id, VisitItem itemModel);
    Task<VisitItem?> DeleteAsync(Guid id);
    Task<List<VisitItem>> GetByVisitIdAsync(Guid visitId);
}
