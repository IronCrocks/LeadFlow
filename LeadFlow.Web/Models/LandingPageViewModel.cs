namespace LeadFlow.Models;

public sealed class LandingPageViewModel
{
    public CreateLeadViewModel Lead { get; init; } = new();

    public IReadOnlyList<string> Services { get; init; } = [];
}
