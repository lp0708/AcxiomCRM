using AcxiomCRM.Data;
using AcxiomCRM.Models;
using AcxiomCRM.Services;
using AcxiomCRM.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Controllers;

public class ActivitiesController : CrmControllerBase
{
    public ActivitiesController(ApplicationDbContext db, UserManager<IdentityUser> users) : base(db, users) { }

    public async Task<IActionResult> Index(ActivityType? type, DateTime? date, ActivityStatus? status, string? assignedTo)
    {
        var query = Db.Activities.VisibleTo(User)
            .Include(a => a.Customer).Include(a => a.Lead).Include(a => a.AssignedUser).AsQueryable();
        if (type != null) query = query.Where(a => a.ActivityType == type);
        if (date != null) query = query.Where(a => a.ActivityDate.Date == date.Value.Date);
        if (status != null) query = query.Where(a => a.Status == status);
        if (!string.IsNullOrEmpty(assignedTo)) query = query.Where(a => a.AssignedTo == assignedTo);

        ViewBag.Type = type;
        ViewBag.Date = date?.ToString("yyyy-MM-dd");
        ViewBag.Status = status;
        ViewBag.AssignedTo = assignedTo;
        await LoadAssigneesAsync();
        return View(await query.OrderByDescending(a => a.ActivityDate).ToListAsync());
    }

    public async Task<IActionResult> Create(int? customerId, int? leadId)
    {
        await LoadFormAsync();
        return View("Edit", new ActivityFormViewModel { CustomerId = customerId, LeadId = leadId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ActivityFormViewModel form)
    {
        form.Id = 0;
        await ValidateRelatedAsync(form.CustomerId, form.LeadId);
        await ApplyAssignmentAsync(form);
        if (!ModelState.IsValid)
        {
            await LoadFormAsync();
            return View("Edit", form);
        }

        var activity = new Activity();
        form.ApplyTo(activity);
        Db.Activities.Add(activity);
        await Db.SaveChangesAsync();
        TempData["Success"] = "Activity logged.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var activity = await Db.Activities.VisibleTo(User).FirstOrDefaultAsync(a => a.Id == id);
        if (activity == null) return NotFound();
        await LoadFormAsync();
        return View(ActivityFormViewModel.From(activity));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, ActivityFormViewModel form)
    {
        var activity = await Db.Activities.VisibleTo(User).FirstOrDefaultAsync(a => a.Id == id);
        if (activity == null) return NotFound();

        form.Id = id;
        await ValidateRelatedAsync(form.CustomerId, form.LeadId);
        await ApplyAssignmentAsync(form);
        if (!ModelState.IsValid)
        {
            await LoadFormAsync();
            return View(form);
        }

        form.ApplyTo(activity);
        await Db.SaveChangesAsync();
        TempData["Success"] = "Activity updated.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = Roles.AdminOrManager)]
    public async Task<IActionResult> Delete(int id)
    {
        var activity = await Db.Activities.FindAsync(id);
        if (activity == null) return NotFound();
        Db.Activities.Remove(activity);
        await Db.SaveChangesAsync();
        TempData["Success"] = "Activity deleted.";
        return RedirectToAction(nameof(Index));
    }

    private async Task LoadFormAsync()
    {
        await LoadCustomersAndLeadsAsync();
        await LoadAssigneesAsync();
    }
}
