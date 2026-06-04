using System;
using lapo_vms_api.Dtos.Visit;
using lapo_vms_api.Helpers;
using lapo_vms_api.Model;

namespace lapo_vms_api.Interface;

public interface IVisitRepository
{
    Task<List<Visit>> GetAllAsync(QueryParameters queryParameters);
    Task<Visit?> GetByIdAsync(Guid id);
    Task<Visit> CreateAsync(Visit visitModel);
    Task<Visit?> UpdateAsync(Guid id, Visit visitModel);

    Task<Visit?> UpdateTagNumberAsync(Guid visitId, string tagNumber);
    Task<Visit?> UpdateStatusAsync(Guid visitId, VisitStatus status);
    Task<Visit?> CheckInAsync(Guid visitId, DateTime checkInTime, string checkedInBy);
    Task<Visit?> CheckOutAsync(Guid visitId, DateTime checkOutTime, string checkedOutBy);
    Task<Visit?> RescheduleAsync(Guid visitId, DateTime rescheduleDate);
    Task<Visit?> DeleteAsync(Guid id);

    Task<List<Visit>> GetByVisitorIdAsync(Guid visitorId);
    Task<List<Visit>> GetByStatusAsync(VisitStatus status);
    Task<List<Visit>> GuestSearchAsync(string search);

    Task<List<ExportVisitsDto>?> GetVisitsForExportAsync(VisitExportRequest request);

    Task<bool> HasActiveVisitByPhoneAsync(string normalizedPhone);
    Task<bool> IsTagNumberInUseAsync(string tagNumber, Guid excludeVisitId);
}
