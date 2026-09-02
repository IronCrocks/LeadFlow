namespace LeadFlow.Application.Leads;

public sealed class LeadFlowOptions
{
    public const string SectionName = "LeadFlow";

    public List<string> Services { get; set; } = [];
}
