using AcxiomCRM.Data;
using AcxiomCRM.Models;
using AcxiomCRM.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Controllers;

public class ActivitiesController : CrmControllerBase
{
    private static readonly string[] EditableFields =
        { nameof(Activity.ActivityType), nameof(Activity.Subject), nameof(Activity.Description), nameof(Activity.ActivityDate),
          nameof(Activity.CustomerId), nameof(Activity.LeadId), nameof(Activity.Status) };

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
        return View("Edit", new Activity { CustomerId = customerId, LeadId = leadId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("ActivityType,Subject,Description,ActivityDate,CustomerId,LeadId,Status,AssignedTo")] Activity activity)
    {
        await ValidateRelatedAsync(activity.CustomerId, activity.LeadId);
        await ApplyAssignmentAsync(activity);
        if (!ModelState.IsValid)
        {
            await LoadFormAsync();
            return View("Edit", activity);
        }

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
        return View(activity);
    }

    [HttpPost, ActionName("Edit"), ValidateAntiForgeryToken]
    public async Task<IActionResult> EditPost(int id)
    {
        var activity = await Db.Activities.VisibleTo(User).FirstOrDefaultAsync(a => a.Id == id);
        if (activity == null) return NotFound();

        var fields = User.IsSalesOnly() ? EditableFields : EditableFields.Append(nameof(Activity.AssignedTo)).ToArray();
        await TryUpdateModelAsync(activity, "", m => fields.Contains(m.PropertyName));
        await ValidateRelatedAsync(activity.CustomerId, activity.LeadId);
        await ApplyAssignmentAsync(activity);
        if (!ModelState.IsValid)
        {
            await LoadFormAsync();
            return View(activity);
        }

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
