namespace LeadFlow.Application.Authentication;

public interface IManagerAuthenticationService
{
    Task<bool> SignInAsync(string email, string password);

    Task SignOutAsync();
}
