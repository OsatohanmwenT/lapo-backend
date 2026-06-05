using lapo_vms_api.Data;
using lapo_vms_api.Dtos.Visitor;
using lapo_vms_api.Helpers;
using lapo_vms_api.Interface;
using lapo_vms_api.Model;
using Microsoft.EntityFrameworkCore;


namespace lapo_vms_api.Repository;

public class VisitorRepository(ApplicationDBContext context) : IVisitorRepository
{
    private readonly ApplicationDBContext _context = context;

    public async Task<bool> IsVisitorPhoneExistsAsync(string normalizedPhone)
    {
        return await _context.Visitor.AnyAsync(v =>
            v.PhoneNumber
                .Replace(" ", "")
                .Replace("+", "")
                .Replace("-", "")
                .Replace("(", "")
                .Replace(")", "") == normalizedPhone);
    }

    public async Task<Visitor> CreateAsync(Visitor visitorModel)
    {
        visitorModel.CreatedAt = WatClock.Now;

        await _context.Visitor.AddAsync(visitorModel);
        await _context.SaveChangesAsync();
        return visitorModel;
    }

    public async Task<Visitor?> DeleteAsync(Guid id)
    {
        var visitor = await _context.Visitor.FindAsync(id);
        if (visitor == null) return null;

        _context.Visitor.Remove(visitor);
        await _context.SaveChangesAsync();
        return visitor;
    }

    public async Task<List<Visitor>> GetAllAsync(QueryParameters queryParameters)
    {
        var visitors = _context.Visitor
            .Include(v => v.Visits)
            .ThenInclude(visit => visit.VisitItems)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(queryParameters.Search))
        {
            visitors = visitors.Where(v =>
                v.FullName.Contains(queryParameters.Search));
        }

        if (!string.IsNullOrWhiteSpace(queryParameters.Status))
        {
            if (Enum.TryParse<VisitStatus>(queryParameters.Status, true, out var status))
            {
                visitors = visitors.Where(v => v.Visits.Any(visit => visit.Status == status));
            }
        }



        return await visitors
            .OrderByDescending(v => v.CreatedAt)
            .Skip((queryParameters.PageNumber - 1) * queryParameters.PageSize)
            .Take(queryParameters.PageSize)
            .ToListAsync();
    }

    public async Task<Visitor?> GetByIdAsync(Guid id)
    {
        return await _context.Visitor
        .Include(v => v.Visits)
        .Include(v => v.Identification)
        .Include(v => v.WorkerDetails).FirstOrDefaultAsync(v => v.Id == id);
    }

    public async Task<Visitor?> GetByPhoneNumberAsync(string phoneNumber)
    {
        return await _context.Visitor
            .Include(v => v.Visits)
            .Include(v => v.Identification)
            .Include(v => v.WorkerDetails)
            .Where(v => v.PhoneNumber
                .Replace(" ", "")
                .Replace("+", "")
                .Replace("-", "")
                .Replace("(", "")
                .Replace(")", "") == phoneNumber)
            .OrderByDescending(v => v.CreatedAt)
            .FirstOrDefaultAsync();
    }

    public async Task<Visitor?> UpdateAsync(Guid id, Visitor visitorModel)
    {
        var existing = await _context.Visitor.FindAsync(id);
        if (existing == null) return null;

        existing.FullName = visitorModel.FullName;
        existing.PhoneNumber = visitorModel.PhoneNumber;

        await _context.SaveChangesAsync();
        return existing;
    }

    public async Task<List<VisitorExportDto>> GetVisitorsForExportAsync(VisitorExportRequest request)
    {
        var query = _context.Visit
            .Include(v => v.Visitor)
                .ThenInclude(v => v.WorkerDetails)
            .Include(v => v.Visitor)
                .ThenInclude(v => v.Identification)
            .AsQueryable();

        if (request.StartDate.HasValue)
        {
            query = query.Where(v => v.RegisteredAt >= request.StartDate.Value.Date);
        }

        if (request.EndDate.HasValue)
        {
            query = query.Where(v => v.RegisteredAt < request.EndDate.Value.Date.AddDays(1));
        }

        return await query
            .OrderByDescending(v => v.RegisteredAt)
            .Select(v => new VisitorExportDto
            {
                FullName = v.Visitor.FullName,
                PhoneNumber = v.Visitor.PhoneNumber,
                VisitorType = v.Visitor.VisitorType.ToString(),
                CompanyName = v.Visitor.WorkerDetails != null ? v.Visitor.WorkerDetails.CompanyName : string.Empty,
                IdentificationType = v.Visitor.Identification != null ? v.Visitor.Identification.IdentificationType : string.Empty,
                IdentificationNumber = v.Visitor.Identification != null ? v.Visitor.Identification.IdentificationNumber : string.Empty,
                HostName = v.HostName,
                HostDepartment = v.HostDepartment,
                TagNumber = v.TagNumber,
                PurposeOfVisit = v.PurposeOfVisit,
                FloorNumber = v.FloorNumber,
                CheckInTime = v.CheckInTime,
                CheckOutTime = v.CheckOutTime,
                Status = v.Status.ToString(),
                RegisteredAt = v.RegisteredAt
            })
            .ToListAsync();
    }
}
