using System.Text.Json;
using AcxiomCRM.Data;
using AcxiomCRM.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace AcxiomCRM.Services;

/// <summary>
/// Gives every self-registered user the least-privileged role (SalesExecutive) and audits
/// registration and password changes. Passwords and tokens are never written to the audit log.
/// </summary>
public class AppUserManager : UserManager<IdentityUser>
{
    private readonly ApplicationDbContext _db;

    public AppUserManager(IUserStore<IdentityUser> store, IOptions<IdentityOptions> optionsAccessor,
        IPasswordHasher<IdentityUser> passwordHasher, IEnumerable<IUserValidator<IdentityUser>> userValidators,
        IEnumerable<IPasswordValidator<IdentityUser>> passwordValidators, ILookupNormalizer keyNormalizer,
        IdentityErrorDescriber errors, IServiceProvider services, ILogger<UserManager<IdentityUser>> logger,
        ApplicationDbContext db)
        : base(store, optionsAccessor, passwordHasher, userValidators, passwordValidators, keyNormalizer, errors, services, logger)
    {
        _db = db;
    }

    // The Identity UI Register page calls this overload; the seeder uses CreateAsync(user) + AddPasswordAsync instead.
    public override async Task<IdentityResult> CreateAsync(IdentityUser user, string password)
    {
        var result = await base.CreateAsync(user, password);
        if (result.Succeeded)
        {
            await AddToRoleAsync(user, Roles.SalesExecutive);
            await AuditAsync("Register", user, JsonSerializer.Serialize(new { user.UserName, Role = Roles.SalesExecutive }));
        }
        return result;
    }

    public override async Task<IdentityResult> ChangePasswordAsync(IdentityUser user, string currentPassword, string newPassword)
    {
        var result = await base.ChangePasswordAsync(user, currentPassword, newPassword);
        await AuditAsync(result.Succeeded ? "PasswordChange" : "PasswordChangeFailed", user);
        return result;
    }

    public override async Task<IdentityResult> ResetPasswordAsync(IdentityUser user, string token, string newPassword)
    {
        var result = await base.ResetPasswordAsync(user, token, newPassword);
        await AuditAsync(result.Succeeded ? "PasswordReset" : "PasswordResetFailed", user);
        return result;
    }

    private async Task AuditAsync(string action, IdentityUser user, string? newValue = null)
    {
        _db.AddAudit(action, "User", user.Id, newValue: newValue, userId: user.Id, userName: user.UserName);
        await _db.SaveChangesAsync();
    }
}
