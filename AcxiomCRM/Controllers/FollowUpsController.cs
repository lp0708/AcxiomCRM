using AcxiomCRM.Data;
using AcxiomCRM.Models;
using AcxiomCRM.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Controllers;

public class FollowUpsController : CrmControllerBase
{
    private static readonly string[] EditableFields =
        { nameof(FollowUp.CustomerId), nameof(FollowUp.LeadId), nameof(FollowUp.FollowUpDate), nameof(FollowUp.FollowUpType),
          nameof(FollowUp.Remarks), nameof(FollowUp.Status) };

    public FollowUpsController(ApplicationDbContext db, UserManager<IdentityUser> users) : base(db, users) { }

    /// <param name="view">"pending" (planned), "overdue" or empty for all.</param>
    public async Task<IActionResult> Index(string? view, FollowUpStatus? status, DateTime? date, string? assignedTo, string? q)
    {
        var query = Db.FollowUps.VisibleTo(User)
            .Include(f => f.Customer).Include(f => f.Lead).Include(f => f.AssignedUser).AsQueryable();
        var today = DateTime.Today;
        if (view == "pending") query = query.Where(f => f.Status == FollowUpStatus.Planned);
        if (view == "overdue") query = query.Where(f => f.Status == FollowUpStatus.Planned && f.FollowUpDate < today);
        if (status != null) query = query.Where(f => f.Status == status);
        if (date != null) query = query.Where(f => f.FollowUpDate.Date == date.Value.Date);
        if (!string.IsNullOrEmpty(assignedTo)) query = query.Where(f => f.AssignedTo == assignedTo);
        if (!string.IsNullOrWhiteSpace(q))
        {
            var like = $"%{q.Trim()}%";
            query = query.Where(f => EF.Functions.Like(f.Customer!.CustomerName, like) || EF.Functions.Like(f.Lead!.LeadName, like));
        }

        ViewBag.View = view;
        ViewBag.Status = status;
        ViewBag.Date = date?.ToString("yyyy-MM-dd");
        ViewBag.AssignedTo = assignedTo;
        ViewBag.Q = q;
        await LoadAssigneesAsync();
        return View(await query.OrderBy(f => f.FollowUpDate).ToListAsync());
    }

    public async Task<IActionResult> Create(int? customerId, int? leadId)
    {
        await LoadFormAsync();
        return View("Edit", new FollowUp { CustomerId = customerId, LeadId = leadId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("CustomerId,LeadId,FollowUpDate,FollowUpType,Remarks,AssignedTo")] FollowUp followUp)
    {
        // A newly scheduled follow-up always starts as Planned, so the "not before today" rule always applies.
        followUp.Status = FollowUpStatus.Planned;
        await ValidateRelatedAsync(followUp.CustomerId, followUp.LeadId);
        await ApplyAssignmentAsync(followUp);
        if (!ModelState.IsValid)
        {
            await LoadFormAsync();
            return View("Edit", followUp);
        }

        Db.FollowUps.Add(followUp);
        await Db.SaveChangesAsync();
        TempData["Success"] = "Follow-up scheduled.";
        return RedirectToAction(nameof(Index), new { view = "pending" });
    }

    public async Task<IActionResult> Edit(int id)
    {
        var followUp = await Db.FollowUps.VisibleTo(User).FirstOrDefaultAsync(f => f.Id == id);
        if (followUp == null) return NotFound();
        await LoadFormAsync();
        return View(followUp);
    }

    [HttpPost, ActionName("Edit"), ValidateAntiForgeryToken]
    public async Task<IActionResult> EditPost(int id)
    {
        var followUp = await Db.FollowUps.VisibleTo(User).FirstOrDefaultAsync(f => f.Id == id);
        if (followUp == null) return NotFound();

        var fields = User.IsSalesOnly() ? EditableFields : EditableFields.Append(nameof(FollowUp.AssignedTo)).ToArray();
        await TryUpdateModelAsync(followUp, "", m => fields.Contains(m.PropertyName));
        await ValidateRelatedAsync(followUp.CustomerId, followUp.LeadId);
        await ApplyAssignmentAsync(followUp);
        if (!ModelState.IsValid)
        {
            await LoadFormAsync();
            return View(followUp);
        }

        await Db.SaveChangesAsync();
        TempData["Success"] = "Follow-up updated.";
        return RedirectToAction(nameof(Index));
    }

    /// <summary>Marks a follow-up Completed or Missed from the list page.</summary>
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SetStatus(int id, FollowUpStatus status)
    {
        var followUp = await Db.FollowUps.VisibleTo(User).FirstOrDefaultAsync(f => f.Id == id);
        if (followUp == null) return NotFound();
        if (status is not (FollowUpStatus.Completed or FollowUpStatus.Missed or FollowUpStatus.Cancelled))
            return BadRequest();

        followUp.Status = status;
        await Db.SaveChangesAsync();
        TempData["Success"] = $"Follow-up marked {status}.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = Roles.AdminOrManager)]
    public async Task<IActionResult> Delete(int id)
    {
        var followUp = await Db.FollowUps.FindAsync(id);
        if (followUp == null) return NotFound();
        Db.FollowUps.Remove(followUp);
        await Db.SaveChangesAsync();
        TempData["Success"] = "Follow-up deleted.";
        return RedirectToAction(nameof(Index));
    }

    private async Task LoadFormAsync()
    {
        await LoadCustomersAndLeadsAsync();
        await LoadAssigneesAsync();
    }
}
