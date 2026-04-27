using System;
using lapo_vms_api.Dtos.Visit;
using lapo_vms_api.Helpers;
using lapo_vms_api.Model;

namespace lapo_vms_api.Interface;

public interface IVisitRepository
{
    Task<List<Visit>> GetAllAsync(QueryParameters queryParameters);
    Task<Visit?> GetByIdAsync(int id);
    Task<Visit> CreateAsync(Visit visitModel);
    Task<Visit?> UpdateAsync(int id, Visit visitModel);

    Task<Visit?> UpdateTagNumberAsync(int visitId, string tagNumber);
    Task<Visit?> UpdateStatusAsync(int visitId, VisitStatus status);
    Task<Visit?> CheckOutAsync(int visitId, DateTime checkOutTime, string checkedOutBy);
    Task<Visit?> RescheduleAsync(int visitId, DateTime rescheduleDate);
    Task<Visit?> DeleteAsync(int id);

    Task<List<Visit>> GetByVisitorIdAsync(int visitorId);
    Task<List<Visit>> GetByStatusAsync(VisitStatus status);

    Task<List<ExportVisitsDto>?> GetVisitsForExportAsync(VisitExportRequest request);
}
