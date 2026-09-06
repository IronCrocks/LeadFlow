using LeadFlow.Application.Managers;
using LeadFlow.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LeadFlow.Infrastructure.Identity;

public sealed class IdentityManagerDirectory(ApplicationDbContext dbContext) : IManagerDirectory
{
    public async Task<IReadOnlyDictionary<Guid, string>> GetEmailsAsync(
        IReadOnlyCollection<Guid> managerIds,
        CancellationToken cancellationToken = default)
    {
        var ids = managerIds.Distinct().ToArray();
        if (ids.Length == 0)
        {
            return new Dictionary<Guid, string>();
        }

        return await dbContext.Users
            .AsNoTracking()
            .Where(user => ids.Contains(user.Id) && user.Email != null)
            .ToDictionaryAsync(user => user.Id, user => user.Email!, cancellationToken);
    }
}
