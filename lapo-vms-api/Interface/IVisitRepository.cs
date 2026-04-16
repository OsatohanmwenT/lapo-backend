using System;
using lapo_vms_api.Model;

namespace lapo_vms_api.Interface;

public interface IVisitRepository
{
    Task<List<Visit>> GetAllAsync();
    Task<Visit?> GetByIdAsync(int id);
    Task<Visit> CreateAsync(Visit visitModel);
    Task<Visit?> UpdateAsync(int id, Visit visitModel);

    Task<Visit?> UpdateStatusAsync(int visitId, VisitStatus status);
    Task<Visit?> DeleteAsync(int id);

    Task<List<Visit>> GetByVisitorIdAsync(int visitorId);
    Task<List<Visit>> GetByStatusAsync(VisitStatus status);
}
