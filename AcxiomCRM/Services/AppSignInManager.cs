using System.Text.Json;
using AcxiomCRM.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace AcxiomCRM.Services;

/// <summary>Enforces lockout on every password login and audits login, failed login, lockout and logout.</summary>
public class AppSignInManager : SignInManager<IdentityUser>
{
    private readonly ApplicationDbContext _db;

    public AppSignInManager(UserManager<IdentityUser> userManager, IHttpContextAccessor contextAccessor,
        IUserClaimsPrincipalFactory<IdentityUser> claimsFactory, IOptions<IdentityOptions> optionsAccessor,
        ILogger<SignInManager<IdentityUser>> logger, IAuthenticationSchemeProvider schemes,
        IUserConfirmation<IdentityUser> confirmation, ApplicationDbContext db)
        : base(userManager, contextAccessor, claimsFactory, optionsAccessor, logger, schemes, confirmation)
    {
        _db = db;
    }

    public override async Task<SignInResult> PasswordSignInAsync(string userName, string password, bool isPersistent, bool lockoutOnFailure)
    {
        // The default Identity UI passes lockoutOnFailure: false; the security policy requires lockout.
        var result = await base.PasswordSignInAsync(userName, password, isPersistent, lockoutOnFailure: true);

        var user = await UserManager.FindByNameAsync(userName);
        var action = result.Succeeded ? "Login" : result.IsLockedOut ? "LockedOut" : "LoginFailed";
        // Never log the password; only the attempted user name.
        _db.AddAudit(action, "User", user?.Id, newValue: JsonSerializer.Serialize(new { UserName = userName }),userId: user?.Id, userName: userName);
        await _db.SaveChangesAsync();

        return result;
    }

    public override async Task SignOutAsync()
    {
        var principal = Context.User;
        _db.AddAudit("Logout", "User", principal.UserId());
        await _db.SaveChangesAsync();
        await base.SignOutAsync();
    }
}
