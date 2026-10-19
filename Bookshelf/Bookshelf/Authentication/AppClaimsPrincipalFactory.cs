using System.Security.Claims;
using Bookshelf.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace Bookshelf.Auth;

public class AppClaimsPrincipalFactory(UserManager<AppUser> userManager, IOptions<IdentityOptions> options) : UserClaimsPrincipalFactory<AppUser>(userManager, options)
{
    protected override async Task<ClaimsIdentity> GenerateClaimsAsync(AppUser user)
    {
        var identity = await base.GenerateClaimsAsync(user);
        identity.AddClaim(new Claim(ClaimTypes.GivenName, user.FirstName));
        return identity;
    }
}