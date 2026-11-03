using AcxiomCRM.Data;
using AcxiomCRM.Models;
using AcxiomCRM.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Controllers;

/// <summary>Shared helpers for the CRM controllers: dropdown lists and assignment rules.</summary>
public abstract class CrmControllerBase : Controller
{
    protected readonly ApplicationDbContext Db;
    protected readonly UserManager<IdentityUser> Users;

    protected CrmControllerBase(ApplicationDbContext db, UserManager<IdentityUser> users)
    {
        Db = db;
        Users = users;
    }

    /// <summary>Sales Executives always own what they create; Admin/Manager must assign a valid Sales Executive.</summary>
    protected async Task ApplyAssignmentAsync(IAssignable record)
    {
        if (User.IsSalesOnly())
        {
            record.AssignedTo = User.UserId();
            return;
        }

        if (string.IsNullOrEmpty(record.AssignedTo))
        {
            ModelState.AddModelError(nameof(IAssignable.AssignedTo), "Assign the record to a Sales Executive.");
            return;
        }

        var salesIds = (await Users.GetUsersInRoleAsync(Roles.SalesExecutive)).Select(u => u.Id);
        if (!salesIds.Contains(record.AssignedTo))
            ModelState.AddModelError(nameof(IAssignable.AssignedTo), "Select a valid Sales Executive.");
    }

    /// <summary>Checks that referenced customer/lead ids exist and are visible to the current user.</summary>
    protected async Task ValidateRelatedAsync(int? customerId, int? leadId)
    {
        if (customerId != null && !await Db.Customers.VisibleTo(User).AnyAsync(c => c.Id == customerId))
            ModelState.AddModelError("CustomerId", "Select a valid customer.");
        if (leadId != null && !await Db.Leads.VisibleTo(User).AnyAsync(l => l.Id == leadId))
            ModelState.AddModelError("LeadId", "Select a valid lead.");
    }

    protected async Task LoadAssigneesAsync()
    {
        var sales = await Users.GetUsersInRoleAsync(Roles.SalesExecutive);
        ViewBag.Assignees = new SelectList(sales.OrderBy(u => u.UserName), "Id", "UserName");
    }

    protected async Task LoadCustomersAndLeadsAsync()
    {
        ViewBag.Customers = new SelectList(await Db.Customers.VisibleTo(User).OrderBy(c => c.CustomerName).ToListAsync(), "Id", "CustomerName");
        ViewBag.Leads = new SelectList(await Db.Leads.VisibleTo(User).OrderBy(l => l.LeadName).ToListAsync(), "Id", "LeadName");
    }

    protected static string NextCode(string prefix, int lastId) => $"{prefix}-{lastId + 1:D4}";
}
