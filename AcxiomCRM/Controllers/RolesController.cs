using AcxiomCRM.Models;
using AcxiomCRM.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace AcxiomCRM.Controllers;

/// <summary>Read-only view of the three roles and what each may do. Roles are assigned on the Users page.</summary>
[Authorize(Roles = Roles.Admin)]
public class RolesController : Controller
{
    private static readonly Dictionary<string, string> Descriptions = new()
    {
        [Roles.Admin] = "Full administration: users, roles, audit log, all CRM records and reports.",
        [Roles.Manager] = "Manages all team CRM data, pipeline and reports; no security administration.",
        [Roles.SalesExecutive] = "Manages the customers, leads, opportunities, follow-ups and activities assigned to them.",
    };

    private readonly UserManager<IdentityUser> _users;

    public RolesController(UserManager<IdentityUser> users)
    {
        _users = users;
    }

    public async Task<IActionResult> Index()
    {
        var model = new RolesViewModel();
        foreach (var role in Roles.All)
            model.Roles.Add(new RoleSummary(role, Descriptions[role], (await _users.GetUsersInRoleAsync(role)).Count));
        return View(model);
    }
}
