using LeadFlow.Application.Authentication;
using Microsoft.AspNetCore.Identity;

namespace LeadFlow.Infrastructure.Identity;

public sealed class IdentityManagerAuthenticationService(SignInManager<ApplicationUser> signInManager)
    : IManagerAuthenticationService
{
    public async Task<bool> SignInAsync(string email, string password)
    {
        var result = await signInManager.PasswordSignInAsync(
            email,
            password,
            isPersistent: false,
            lockoutOnFailure: true);

        return result.Succeeded;
    }

    public Task SignOutAsync()
    {
        return signInManager.SignOutAsync();
    }
}
