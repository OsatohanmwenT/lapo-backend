using lapo_vms_api.Data;
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

    public async Task<List<Visitor>> GetAllAsync(QueryObject query)
    {
        var visitors = _context.Visitor
            .Include(v => v.Visits)
            .ThenInclude(visit => visit.VisitItems)
            .AsQueryable();
        if (!string.IsNullOrEmpty(query.FullName))
        {
            visitors = visitors.Where(v => v.FullName.Contains(query.FullName));
        }
        return await visitors.ToListAsync();
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
}
