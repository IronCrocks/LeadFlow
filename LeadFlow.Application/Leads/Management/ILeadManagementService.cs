namespace LeadFlow.Application.Leads;

public interface ILeadManagementService
{
    Task<LeadDetails?> GetDetailsAsync(Guid leadId, CancellationToken cancellationToken = default);

    Task<bool> UpdateAsync(Guid leadId, UpdateLeadCommand command, CancellationToken cancellationToken = default);
}
