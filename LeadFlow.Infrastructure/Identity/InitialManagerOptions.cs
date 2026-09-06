namespace LeadFlow.Infrastructure.Identity;

public sealed class InitialManagerOptions
{
    public const string SectionName = "InitialManager";

    public string? Email { get; set; }

    public string? Password { get; set; }
}
