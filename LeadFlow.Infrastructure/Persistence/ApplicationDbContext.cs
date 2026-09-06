using LeadFlow.Application.Persistence;
using LeadFlow.Domain.Entities;
using LeadFlow.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace LeadFlow.Infrastructure.Persistence;

public sealed class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : IdentityUserContext<ApplicationUser, Guid>(options), IApplicationDbContext
{
    public DbSet<Lead> Leads => Set<Lead>();

    public DbSet<LeadStatusHistory> LeadStatusHistory => Set<LeadStatusHistory>();

    public void ClearChangeTracker()
    {
        ChangeTracker.Clear();
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
    }
}
