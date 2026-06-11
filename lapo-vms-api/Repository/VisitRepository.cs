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

    public async Task<Visit?> DeleteAsync(Guid id)
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
            var statusStr = queryParameters.Status.Equals("Approved", StringComparison.OrdinalIgnoreCase)
                ? nameof(VisitStatus.CheckedIn)
                : queryParameters.Status;

            if (Enum.TryParse<VisitStatus>(statusStr, true, out var status))
            {
                query = query.Where(v => v.Status == status);
            }
        }

        if (queryParameters.StartDate.HasValue)
        {
            query = query.Where(v => v.RegisteredAt >= queryParameters.StartDate.Value.Date);
        }

        if (queryParameters.EndDate.HasValue)
        {
            query = query.Where(v => v.RegisteredAt < queryParameters.EndDate.Value.Date.AddDays(1));
        }

        return await query
            .OrderByDescending(v => v.RegisteredAt) // newest first
            .Skip((queryParameters.PageNumber - 1) * queryParameters.PageSize)
            .Take(queryParameters.PageSize)
            .ToListAsync();
    }

    public async Task<Visit?> GetByIdAsync(Guid id)
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

    public async Task<List<Visit>> GuestSearchAsync(string search)
    {
        return await _context.Visit
                 .Include(v => v.Visitor)
                    .ThenInclude(v => v.Identification)
                 .Include(v => v.Visitor)
                    .ThenInclude(v => v.WorkerDetails)
                 .Include(v => v.VisitItems)
                 .Where(v => v.Status == VisitStatus.CheckedIn)
                 .Where(v =>
                    v.Visitor.FullName.Contains(search) ||
                    v.Visitor.PhoneNumber.Contains(search) ||
                    (v.TagNumber != null && v.TagNumber.Contains(search)))
                 .OrderByDescending(v => v.CheckInTime)
                 .Take(10)
                 .ToListAsync();
    }

    public async Task<List<Visit>> GetByVisitorIdAsync(Guid visitorId)
    {
        return await _context.Visit
                .Include(v => v.Visitor)
                    .ThenInclude(v => v.Identification)
                .Include(v => v.Visitor)
                    .ThenInclude(v => v.WorkerDetails)
                .Where(v => v.VisitorId == visitorId)
                .ToListAsync();
    }

    public async Task<Visit?> UpdateAsync(Guid id, Visit visitModel)
    {
        var existing = await _context.Visit.FindAsync(id);
        if (existing == null) return null;

        existing.Status = visitModel.Status;
        existing.CheckOutTime = visitModel.CheckOutTime;
        // existing.CheckedOutBy = visitModel.CheckedOutBy;

        await _context.SaveChangesAsync();
        return existing;
    }

    public async Task<Visit?> UpdateTagNumberAsync(Guid visitId, string tagNumber)
    {
        var existing = await _context.Visit
                .Include(v => v.VisitItems)
                .FirstOrDefaultAsync(v => v.Id == visitId);
        if (existing == null) return null;

        existing.TagNumber = tagNumber;
        await _context.SaveChangesAsync();
        return existing;
    }

    public async Task<Visit?> UpdateStatusAsync(Guid visitId, VisitStatus status)
    {
        var existing = await _context.Visit
                .Where(v => v.Id == visitId && v.Status != VisitStatus.CheckedOut)
                .FirstOrDefaultAsync();
        if (existing == null) return null;

        existing.Status = status;
        await _context.SaveChangesAsync();
        return existing;

    }

    public async Task<Visit?> CheckInAsync(Guid visitId, DateTime checkInTime, string checkedInBy)
    {
        var existing = await _context.Visit
                .FirstOrDefaultAsync(v => v.Id == visitId && (v.Status == VisitStatus.Pending || v.Status == VisitStatus.Rescheduled));
        if (existing == null) return null;

        existing.CheckInTime = checkInTime;
        existing.CheckedInBy = checkedInBy;
        existing.Status = VisitStatus.CheckedIn;

        await _context.SaveChangesAsync();
        return existing;
    }

    public async Task<Visit?> CheckOutAsync(Guid visitId, DateTime checkOutTime, string checkedOutBy)
    {
        var existing = await _context.Visit
                .FirstOrDefaultAsync(v => v.Id == visitId && v.Status == VisitStatus.CheckedIn);
        if (existing == null) return null;

        existing.CheckOutTime = checkOutTime;
        existing.CheckedOutBy = checkedOutBy;
        existing.Status = VisitStatus.CheckedOut;

        await _context.SaveChangesAsync();
        return existing;
    }

    public async Task<Visit?> RescheduleAsync(Guid visitId, DateTime rescheduleDate)
    {
        var existing = await _context.Visit
                .FirstOrDefaultAsync(v => v.Id == visitId);
        if (existing == null) return null;

        existing.RescheduleDate = rescheduleDate;
        existing.Status = VisitStatus.Rescheduled;

        await _context.SaveChangesAsync();
        return existing;
    }

    public async Task<bool> HasActiveVisitByPhoneAsync(string normalizedPhone)
    {
        return await _context.Visit
            .Include(v => v.Visitor)
            .Where(v =>
                (v.Status == VisitStatus.Pending || v.Status == VisitStatus.CheckedIn) &&
                v.Visitor.PhoneNumber
                    .Replace(" ", "").Replace("+", "").Replace("-", "")
                    .Replace("(", "").Replace(")", "") == normalizedPhone)
            .AnyAsync();
    }

    public async Task<bool> IsTagNumberInUseAsync(string tagNumber, Guid excludeVisitId)
    {
        return await _context.Visit
            .Where(v =>
                v.Id != excludeVisitId &&
                v.Status == VisitStatus.CheckedIn &&
                v.TagNumber != null &&
                v.TagNumber == tagNumber)
            .AnyAsync();
    }

    public async Task<List<ExportVisitsDto>?> GetVisitsForExportAsync(VisitExportRequest request)
    {
        var query = _context.Visit
                .Include(v => v.Visitor)
                    .ThenInclude(v => v.WorkerDetails)
                .Include(v => v.Visitor)
                    .ThenInclude(v => v.Identification)
                .Include(v => v.VisitItems)
                .AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            query = query.Where(v => v.Visitor.FullName.Contains(request.Search));
        }

        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            var statusStr = request.Status.Equals("Approved", StringComparison.OrdinalIgnoreCase)
                ? nameof(VisitStatus.CheckedIn)
                : request.Status;

            if (Enum.TryParse<VisitStatus>(statusStr, true, out var status))
            {
                query = query.Where(v => v.Status == status);
            }
        }

        if (request.StartDate.HasValue)
        {
            query = query.Where(v => v.RegisteredAt >= request.StartDate.Value.Date);
        }

        if (request.EndDate.HasValue)
        {
            query = query.Where(v => v.RegisteredAt < request.EndDate.Value.Date.AddDays(1));
        }

        query = query.OrderByDescending(v => v.RegisteredAt);

        if (request.PageNumber.HasValue || request.PageSize.HasValue)
        {
            const int maxPageSize = 100;
            var pageNumber = request.PageNumber.GetValueOrDefault(1);
            var pageSize = request.PageSize.GetValueOrDefault(10);

            pageNumber = pageNumber < 1 ? 1 : pageNumber;
            pageSize = pageSize < 1 ? 10 : Math.Min(pageSize, maxPageSize);

            query = query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize);
        }

        var visits = await query.ToListAsync();

        return visits
        .Select(v => new ExportVisitsDto
        {
            VisitorId = v.VisitorId,
            VisitorName = v.Visitor.FullName,
            VisitorPhoneNumber = v.Visitor.PhoneNumber,
            VisitorType = v.Visitor.VisitorType.ToString(),
            PurposeOfVisit = v.PurposeOfVisit,
            TagNumber = v.TagNumber ?? string.Empty,
            FloorNumber = v.FloorNumber,
            CompanyName = v.Visitor.WorkerDetails != null ? v.Visitor.WorkerDetails.CompanyName : string.Empty,
            IdentificationType = v.Visitor.Identification != null ? v.Visitor.Identification.IdentificationType : string.Empty,
            IdentificationNumber = v.Visitor.Identification != null ? v.Visitor.Identification.IdentificationNumber : string.Empty,

            HostName = v.HostName ?? string.Empty,
            HostDepartment = v.HostDepartment ?? string.Empty,
            VisitItems = string.Join("; ", v.VisitItems.Select(i =>
                $"{i.LaptopModel ?? "Item"}{(string.IsNullOrWhiteSpace(i.SerialNumber) ? string.Empty : $" ({i.SerialNumber})")}")),

            RegisteredAt = v.RegisteredAt,
            RescheduleDate = v.RescheduleDate,
            CheckInTime = v.CheckInTime,
            CheckedInBy = v.CheckedInBy,
            CheckOutTime = v.CheckOutTime,
            CheckedOutBy = v.CheckedOutBy,

            Status = v.Status.ToString()
        })
        .ToList();
    }
}
