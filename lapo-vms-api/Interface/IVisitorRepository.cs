using System;
using lapo_vms_api.Helpers;
using lapo_vms_api.Model;

namespace lapo_vms_api.Interface;

public interface IVisitorRepository
{
    Task<List<Visitor>> GetAllAsync(QueryObject query);
    Task<Visitor?> GetByIdAsync(int id);
    Task<Visitor> CreateAsync(Visitor visitorModel);
    Task<Visitor?> UpdateAsync(int id, Visitor visitorModel);
    Task<Visitor?> DeleteAsync(int id);
}
