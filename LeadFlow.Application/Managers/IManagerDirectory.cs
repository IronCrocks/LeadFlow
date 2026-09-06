namespace LeadFlow.Application.Managers;

public interface IManagerDirectory
{
    Task<IReadOnlyDictionary<Guid, string>> GetEmailsAsync(
        IReadOnlyCollection<Guid> managerIds,
        CancellationToken cancellationToken = default);
}
