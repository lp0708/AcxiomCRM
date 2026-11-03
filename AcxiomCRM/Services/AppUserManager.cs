using AcxiomCRM.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace AcxiomCRM.Services;

/// <summary>Gives every self-registered user the least-privileged role (SalesExecutive).</summary>
public class AppUserManager : UserManager<IdentityUser>
{
    public AppUserManager(IUserStore<IdentityUser> store, IOptions<IdentityOptions> optionsAccessor,
        IPasswordHasher<IdentityUser> passwordHasher, IEnumerable<IUserValidator<IdentityUser>> userValidators,
        IEnumerable<IPasswordValidator<IdentityUser>> passwordValidators, ILookupNormalizer keyNormalizer,
        IdentityErrorDescriber errors, IServiceProvider services, ILogger<UserManager<IdentityUser>> logger)
        : base(store, optionsAccessor, passwordHasher, userValidators, passwordValidators, keyNormalizer, errors, services, logger)
    {
    }

    // The Identity UI Register page calls this overload; the seeder uses CreateAsync(user) + AddPasswordAsync instead.
    public override async Task<IdentityResult> CreateAsync(IdentityUser user, string password)
    {
        var result = await base.CreateAsync(user, password);
        if (result.Succeeded)
            await AddToRoleAsync(user, Roles.SalesExecutive);
        return result;
    }
}
