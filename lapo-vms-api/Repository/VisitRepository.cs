using lapo_vms_api.Data;
using lapo_vms_api.Dtos.Visit;
using lapo_vms_api.Helpers;
using lapo_vms_api.Interface;
using lapo_vms_api.Model;
using Microsoft.EntityFrameworkCore;

namespace lapo_vms_api.Repository;

public class VisitRepository : IVisitRepository
{
    private readonly ApplicationDBContext _context;

    public VisitRepository(ApplicationDBContext context)
    {
        _context = context;
    }

    public async Task<Visit> CreateAsync(Visit visitModel)
    {
        await _context.Visit.AddAsync(visitModel);
        await _context.SaveChangesAsync();
        return visitModel;
    }

    public async Task<Visit?> DeleteAsync(int id)
    {
        var visit = await _context.Visit.FindAsync(id);
        if (visit == null) return null;

        _context.Visit.Remove(visit);
        await _context.SaveChangesAsync();
        return visit;
    }

    public async Task<List<Visit>> GetAllAsync(QueryParameters queryParameters)
    {
        var query = _context.Visit
                .Include(v => v.Visitor)
                    .ThenInclude(v => v.Identification)
                .Include(v => v.Visitor)
                    .ThenInclude(v => v.WorkerDetails)
                .AsQueryable();

        if (!string.IsNullOrWhiteSpace(queryParameters.Search))
        {
            query = query.Where(v => v.Visitor.FullName.Contains(queryParameters.Search));
        }

        if (!string.IsNullOrWhiteSpace(queryParameters.Status))
        {
            if (Enum.TryParse<VisitStatus>(queryParameters.Status, true, out var status))
            {
                query = query.Where(v => v.Status == status);
            }
        }

        return await query
                .OrderByDescending(v => v.RegisteredAt)
                .Skip((queryParameters.PageNumber - 1) * queryParameters.PageSize)
                .Take(queryParameters.PageSize)
                .ToListAsync();
    }

    public async Task<Visit?> GetByIdAsync(int id)
    {
        return await _context.Visit
                    .Include(v => v.Visitor)
                        .ThenInclude(v => v.Identification)
                 .Include(v => v.Visitor)
                        .ThenInclude(v => v.WorkerDetails)
                 .Include(v => v.VisitItems)
                 .FirstOrDefaultAsync(v => v.Id == id);
    }

    public async Task<List<Visit>> GetByStatusAsync(VisitStatus status)
    {
        return await _context.Visit
                 .Include(v => v.Visitor)
                    .ThenInclude(v => v.Identification)
                 .Include(v => v.Visitor)
                    .ThenInclude(v => v.WorkerDetails)
                 .Where(v => v.Status == status)
                 .ToListAsync();
    }

    public async Task<List<Visit>> GetByVisitorIdAsync(int visitorId)
    {
        return await _context.Visit
                .Include(v => v.Visitor)
                    .ThenInclude(v => v.Identification)
                .Include(v => v.Visitor)
                    .ThenInclude(v => v.WorkerDetails)
                .Where(v => v.VisitorId == visitorId)
                .ToListAsync();
    }

    public async Task<Visit?> UpdateAsync(int id, Visit visitModel)
    {
        var existing = await _context.Visit.FindAsync(id);
        if (existing == null) return null;

        existing.Status = visitModel.Status;
        existing.CheckOutTime = visitModel.CheckOutTime;
        // existing.CheckedOutBy = visitModel.CheckedOutBy;

        await _context.SaveChangesAsync();
        return existing;
    }

    public async Task<Visit?> UpdateTagNumberAsync(int visitId, string tagNumber)
    {
        var existing = await _context.Visit
                .Include(v => v.VisitItems)
                .FirstOrDefaultAsync(v => v.Id == visitId);
        if (existing == null) return null;

        existing.TagNumber = tagNumber;
        await _context.SaveChangesAsync();
        return existing;
    }

    public async Task<Visit?> UpdateStatusAsync(int visitId, VisitStatus status)
    {
        var existing = await _context.Visit
                .Where(v => v.Id == visitId && v.Status != VisitStatus.CheckedOut)
                .FirstOrDefaultAsync();
        if (existing == null) return null;

        existing.Status = status;
        await _context.SaveChangesAsync();
        return existing;

    }

    public async Task<Visit?> CheckInAsync(int visitId, DateTime checkInTime, string checkedInBy)
    {
        var existing = await _context.Visit
                .FirstOrDefaultAsync(v => v.Id == visitId && v.Status == VisitStatus.Pending);
        if (existing == null) return null;

        existing.CheckInTime = checkInTime;
        existing.CheckedInBy = checkedInBy;
        existing.Status = VisitStatus.CheckedIn;

        await _context.SaveChangesAsync();
        return existing;
    }

    public async Task<Visit?> CheckOutAsync(int visitId, DateTime checkOutTime, string checkedOutBy)
    {
        var existing = await _context.Visit
                .FirstOrDefaultAsync(v => v.Id == visitId && v.Status != VisitStatus.CheckedOut);
        if (existing == null) return null;

        existing.CheckOutTime = checkOutTime;
        existing.CheckedOutBy = checkedOutBy;
        existing.Status = VisitStatus.CheckedOut;

        await _context.SaveChangesAsync();
        return existing;
    }

    public async Task<Visit?> RescheduleAsync(int visitId, DateTime rescheduleDate)
    {
        var existing = await _context.Visit
                .FirstOrDefaultAsync(v => v.Id == visitId);
        if (existing == null) return null;

        existing.RescheduleDate = rescheduleDate;
        existing.Status = VisitStatus.Rescheduled;

        await _context.SaveChangesAsync();
        return existing;
    }

    public async Task<List<ExportVisitsDto>?> GetVisitsForExportAsync(VisitExportRequest request)
    {
        var query = _context.Visit
                .Include(v => v.Visitor)
                    .ThenInclude(v => v.WorkerDetails)
                .Include(v => v.Visitor)
                    .ThenInclude(v => v.Identification)
                .AsQueryable();

        if (request.StartDate.HasValue)
        {
            query = query.Where(v => v.RegisteredAt >= request.StartDate.Value);
        }

        if (request.EndDate.HasValue)
        {
            query = query.Where(v => v.RegisteredAt <= request.EndDate.Value);
        }

        return await query
        .Select(v => new ExportVisitsDto
        {
            VisitorName = v.Visitor.FullName,
            VisitorPhoneNumber = v.Visitor.PhoneNumber,
            PurposeOfVisit = v.PurposeOfVisit,
            TagNumber = v.TagNumber,
            FloorNumber = v.FloorNumber,
            CompanyName = v.Visitor.WorkerDetails != null ? v.Visitor.WorkerDetails.CompanyName : string.Empty,
            IdentificationType = v.Visitor.Identification != null ? v.Visitor.Identification.IdentificationType : string.Empty,
            IdentificationNumber = v.Visitor.Identification != null ? v.Visitor.Identification.IdentificationNumber : string.Empty,

            HostName = v.HostName,
            HostDepartment = v.HostDepartment,

            RegisteredAt = v.RegisteredAt,
            CheckInTime = v.CheckInTime,
            CheckOutTime = v.CheckOutTime,

            Status = v.Status.ToString()
        })
        .ToListAsync();
    }
}
