using System.Text.Json;
using AcxiomCRM.Data;
using AcxiomCRM.Models;
using AcxiomCRM.Services;
using AcxiomCRM.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Controllers;

/// <summary>User administration. Deactivation is a permanent lockout, so the standard Identity login check enforces it.</summary>
[Authorize(Roles = Roles.Admin)]
public class UsersController : Controller
{
    private readonly UserManager<IdentityUser> _users;
    private readonly ApplicationDbContext _db;

    public UsersController(UserManager<IdentityUser> users, ApplicationDbContext db)
    {
        _users = users;
        _db = db;
    }

    public async Task<IActionResult> Index(string? q)
    {
        var query = _users.Users.AsQueryable();
        if (!string.IsNullOrWhiteSpace(q)) query = query.Where(u => EF.Functions.Like(u.UserName ?? "", $"%{q.Trim()}%"));

        var rows = new List<UserRowViewModel>();
        foreach (var user in await query.OrderBy(u => u.UserName).ToListAsync())
        {
            rows.Add(new UserRowViewModel
            {
                Id = user.Id,
                UserName = user.UserName,
                Role = (await _users.GetRolesAsync(user)).FirstOrDefault(),
                IsActive = user.LockoutEnd != DateTimeOffset.MaxValue,
                IsLockedOut = user.LockoutEnd > DateTimeOffset.UtcNow && user.LockoutEnd != DateTimeOffset.MaxValue,
                FailedLoginCount = user.AccessFailedCount
            });
        }

        ViewBag.Q = q;
        return View(rows);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SetRole(string id, string role)
    {
        var user = await _users.FindByIdAsync(id);
        if (user == null || !Roles.All.Contains(role)) return BadRequest();
        if (user.Id == User.UserId())
        {
            TempData["Error"] = "You cannot change your own role.";
            return RedirectToAction(nameof(Index));
        }

        var oldRoles = await _users.GetRolesAsync(user);
        await _users.RemoveFromRolesAsync(user, oldRoles);
        await _users.AddToRoleAsync(user, role);
        await _users.UpdateSecurityStampAsync(user);

        _db.AddAudit("RoleChange", "User", user.Id, JsonSerializer.Serialize(new { Role = oldRoles.FirstOrDefault() }), JsonSerializer.Serialize(new { Role = role }));
        await _db.SaveChangesAsync();
        TempData["Success"] = $"{user.UserName} is now {role}.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SetActive(string id, bool active)
    {
        var user = await _users.FindByIdAsync(id);
        if (user == null) return BadRequest();
        if (user.Id == User.UserId())
        {
            TempData["Error"] = "You cannot deactivate your own account.";
            return RedirectToAction(nameof(Index));
        }

        await _users.SetLockoutEnabledAsync(user, true);
        await _users.SetLockoutEndDateAsync(user, active ? null : DateTimeOffset.MaxValue);
        // Invalidate existing sessions of a deactivated user.
        await _users.UpdateSecurityStampAsync(user);

        _db.AddAudit(active ? "Activate" : "Deactivate", "User", user.Id);
        await _db.SaveChangesAsync();
        TempData["Success"] = $"{user.UserName} {(active ? "activated" : "deactivated")}.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Unlock(string id)
    {
        var user = await _users.FindByIdAsync(id);
        if (user == null) return BadRequest();

        await _users.SetLockoutEndDateAsync(user, null);
        await _users.ResetAccessFailedCountAsync(user);

        _db.AddAudit("Unlock", "User", user.Id);
        await _db.SaveChangesAsync();
        TempData["Success"] = $"{user.UserName} unlocked.";
        return RedirectToAction(nameof(Index));
    }
}
