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

    public async Task<Visitor> CreateAsync(Visitor visitorModel)
    {
        visitorModel.CreatedAt = DateTime.UtcNow;

        await _context.Visitor.AddAsync(visitorModel);
        await _context.SaveChangesAsync();
        return visitorModel;
    }

    public async Task<Visitor?> DeleteAsync(int id)
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
            var match = Enum.GetValues<VisitStatus>()
                .Cast<VisitStatus>()
                .FirstOrDefault(s => s.ToString().Contains(queryParameters.Status,
                    StringComparison.OrdinalIgnoreCase));

            if (Enum.IsDefined(typeof(VisitStatus), match))
            {
                visitors = visitors.Where(v => v.Visits.Any(visit => visit.Status == match));
            }
        }


        return await visitors
            .OrderByDescending(v => v.CreatedAt)
            .Skip((queryParameters.PageNumber - 1) * queryParameters.PageSize)
            .Take(queryParameters.PageSize)
            .ToListAsync();
    }

    public async Task<Visitor?> GetByIdAsync(int id)
    {
        return await _context.Visitor
        .Include(v => v.Visits)
        .Include(v => v.Identification)
        .Include(v => v.WorkerDetails).FirstOrDefaultAsync(v => v.Id == id);
    }

    public async Task<Visitor?> UpdateAsync(int id, Visitor visitorModel)
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
        var visitors = _context.Visitor.AsQueryable();

        if (request.StartDate.HasValue)
        {
            visitors = visitors.Where(v => v.CreatedAt >= request.StartDate.Value);
        }

        if (request.EndDate.HasValue)
        {
            visitors = visitors.Where(v => v.CreatedAt <= request.EndDate.Value);
        }

        return await visitors
            .OrderByDescending(v => v.CreatedAt)
            .Select(v => new VisitorExportDto
            {
                FullName = v.FullName,
                PhoneNumber = v.PhoneNumber,
                VisitorType = v.VisitorType.ToString(),
                CompanyName = v.WorkerDetails != null ? v.WorkerDetails.CompanyName : string.Empty,
                IdentificationType = v.Identification != null ? v.Identification.IdentificationType : string.Empty,
                IdentificationNumber = v.Identification != null ? v.Identification.IdentificationNumber : string.Empty,
                CreatedAt = v.CreatedAt
            })
            .ToListAsync();
    }
}
