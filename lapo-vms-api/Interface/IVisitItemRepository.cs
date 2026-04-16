using System;
using lapo_vms_api.Model;

namespace lapo_vms_api.Interface;

public interface IVisitItemRepository
{
    Task<List<VisitItem>> GetAllAsync();
    Task<VisitItem?> GetByIdAsync(int id);
    Task<VisitItem> CreateAsync(VisitItem itemModel);
    Task<VisitItem?> UpdateAsync(int id, VisitItem itemModel);
    Task<VisitItem?> DeleteAsync(int id);
    Task<List<VisitItem>> GetByVisitIdAsync(int visitId);
}
