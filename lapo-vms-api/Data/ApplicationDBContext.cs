using lapo_vms_api.Model;
using Microsoft.EntityFrameworkCore;

namespace lapo_vms_api.Data;

public class ApplicationDBContext(DbContextOptions<ApplicationDBContext> options) : DbContext(options)
{
    public DbSet<Visit> Visit => Set<Visit>();
    public DbSet<Visitor> Visitor => Set<Visitor>();
    public DbSet<VisitItem> VisitItem => Set<VisitItem>();
    public DbSet<VisitorIdentification> VisitorIdentification => Set<VisitorIdentification>();
    public DbSet<WorkerDetails> WorkerDetails => Set<WorkerDetails>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Visit>()
            .Property(v => v.Status)
            .HasConversion<string>();

        modelBuilder.Entity<Visitor>()
            .Property(v => v.VisitorType)
            .HasConversion<string>();

        modelBuilder.Entity<User>()
            .Property(u => u.Role)
            .HasConversion<string>();
    }
}
