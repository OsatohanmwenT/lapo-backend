using System;
using lapo_vms_api.Dtos.Visitor;
using lapo_vms_api.Helpers;
using lapo_vms_api.Model;

namespace lapo_vms_api.Interface;

public interface IVisitorRepository
{
    Task<List<Visitor>> GetAllAsync(QueryParameters queryParameters);
    Task<Visitor?> GetByIdAsync(Guid id);
    Task<Visitor?> GetByPhoneNumberAsync(string phoneNumber);
    Task<Visitor> CreateAsync(Visitor visitorModel);
    Task<Visitor?> UpdateAsync(Guid id, Visitor visitorModel);
    Task<Visitor?> DeleteAsync(Guid id);
    Task<List<VisitorExportDto>> GetVisitorsForExportAsync(VisitorExportRequest request);
}
