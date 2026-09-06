using LeadFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace LeadFlow.Application.Persistence;

public interface IApplicationDbContext
{
    DbSet<Lead> Leads { get; }

    DbSet<LeadStatusHistory> LeadStatusHistory { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    void ClearChangeTracker();
}
