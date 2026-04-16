using lapo_vms_api.Data;
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

    public async Task<List<Visit>> GetAllAsync()
    {
        return await _context.Visit
                .Include(v => v.VisitItems)
                .ToListAsync();
    }

    public async Task<Visit?> GetByIdAsync(int id)
    {
        return await _context.Visit
                    .Include(v => v.Visitor)
                 .Include(v => v.VisitItems)
                 .FirstOrDefaultAsync(v => v.Id == id);
    }

    public async Task<List<Visit>> GetByStatusAsync(VisitStatus status)
    {
        return await _context.Visit
                 .Where(v => v.Status == status)
                 .ToListAsync();
    }

    public async Task<List<Visit>> GetByVisitorIdAsync(int visitorId)
    {
        return await _context.Visit
                .Where(v => v.VisitorId == visitorId)
                .ToListAsync();
    }

    public async Task<Visit?> UpdateAsync(int id, Visit visitModel)
    {
        var existing = await _context.Visit.FindAsync(id);
        if (existing == null) return null;

        existing.Status = visitModel.Status;
        existing.CheckOutTime = visitModel.CheckOutTime;
        existing.CheckedOutBy = visitModel.CheckedOutBy;

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
}
