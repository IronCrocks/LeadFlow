namespace LeadFlow.Application.Leads;

public interface ILeadCreationService
{
    Task CreateAsync(CreateLeadCommand command, CancellationToken cancellationToken = default);
}
