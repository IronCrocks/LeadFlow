namespace LeadFlow.Infrastructure.Identity;

public static class InitialManagerConfiguration
{
    public static void Validate(string? email, string? password, string? connectionString)
    {
        var hasEmail = !string.IsNullOrWhiteSpace(email);
        var hasPassword = !string.IsNullOrWhiteSpace(password);

        if (!hasEmail && !hasPassword)
        {
            return;
        }

        if (!hasEmail || !hasPassword)
        {
            throw new InvalidOperationException(
                "InitialManager:Email and InitialManager:Password must be set together.");
        }

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "ConnectionStrings:DefaultConnection must be set when InitialManager is configured.");
        }
    }
}
