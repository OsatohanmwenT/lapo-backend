using lapo_vms_api.Data;
using lapo_vms_api.Interface;
using lapo_vms_api.Model;
using Microsoft.EntityFrameworkCore;

namespace lapo_vms_api.Repository;

public class VisitItemRepository : IVisitItemRepository

{
    private readonly ApplicationDBContext _context;

    public VisitItemRepository(ApplicationDBContext context)
    {
        _context = context;
    }

    public async Task<VisitItem> CreateAsync(VisitItem itemModel)
    {
        await _context.VisitItem.AddAsync(itemModel);
        await _context.SaveChangesAsync();
        return itemModel;
    }

    public async Task<VisitItem?> DeleteAsync(int id)
    {
        var item = await _context.VisitItem.FindAsync(id);
        if (item == null) return null;

        _context.VisitItem.Remove(item);
        await _context.SaveChangesAsync();
        return item;
    }

    public async Task<List<VisitItem>> GetAllAsync()
    {
        return await _context.VisitItem.ToListAsync();
    }

    public async Task<VisitItem?> GetByIdAsync(int id)
    {
        return await _context.VisitItem.FindAsync(id);
    }

    public async Task<List<VisitItem>> GetByVisitIdAsync(int visitId)
    {
        return await _context.VisitItem
                .Where(vi => vi.VisitId == visitId)
                .ToListAsync();
    }

    public async Task<VisitItem?> UpdateAsync(int id, VisitItem itemModel)
    {
        var existing = await _context.VisitItem.FindAsync(id);
        if (existing == null) return null;

        existing.SerialNumber = itemModel.SerialNumber;
        existing.LaptopModel = itemModel.LaptopModel;

        await _context.SaveChangesAsync();
        return existing;
    }
}
