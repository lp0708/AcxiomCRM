using AcxiomCRM.Data;
using AcxiomCRM.Models;
using AcxiomCRM.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Controllers;

public class OpportunitiesController : CrmControllerBase
{
    private static readonly string[] EditableFields =
        { nameof(Opportunity.OpportunityName), nameof(Opportunity.CustomerId), nameof(Opportunity.Amount), nameof(Opportunity.Stage),
          nameof(Opportunity.Probability), nameof(Opportunity.ExpectedCloseDate), nameof(Opportunity.Notes) };

    public OpportunitiesController(ApplicationDbContext db, UserManager<IdentityUser> users) : base(db, users) { }

    public async Task<IActionResult> Index(string? q, OpportunityStage? stage, string? status)
    {
        var query = Db.Opportunities.VisibleTo(User).Include(o => o.Customer).Include(o => o.AssignedUser).AsQueryable();
        if (!string.IsNullOrWhiteSpace(q))
        {
            var like = $"%{q.Trim()}%";
            query = query.Where(o => EF.Functions.Like(o.OpportunityName, like) || EF.Functions.Like(o.Customer!.CustomerName, like));
        }
        if (stage != null) query = query.Where(o => o.Stage == stage);
        query = status switch
        {
            "Open" => query.Where(o => o.Stage != OpportunityStage.Won && o.Stage != OpportunityStage.Lost),
            "Won" => query.Where(o => o.Stage == OpportunityStage.Won),
            "Lost" => query.Where(o => o.Stage == OpportunityStage.Lost),
            _ => query
        };

        ViewBag.Q = q;
        ViewBag.Stage = stage;
        ViewBag.StatusFilter = status;
        return View(await query.OrderBy(o => o.ExpectedCloseDate).ToListAsync());
    }

    public async Task<IActionResult> Details(int id)
    {
        var opportunity = await Db.Opportunities.VisibleTo(User)
            .Include(o => o.Customer).Include(o => o.Lead).Include(o => o.AssignedUser)
            .FirstOrDefaultAsync(o => o.Id == id);
        return opportunity == null ? NotFound() : View(opportunity);
    }

    public async Task<IActionResult> Create(int? customerId)
    {
        await LoadFormAsync();
        return View("Edit", new Opportunity { CustomerId = customerId ?? 0 });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("OpportunityName,CustomerId,Amount,Stage,Probability,ExpectedCloseDate,Notes,AssignedTo")] Opportunity opportunity)
    {
        await ValidateRelatedAsync(opportunity.CustomerId, null);
        await ApplyAssignmentAsync(opportunity);
        if (!ModelState.IsValid)
        {
            await LoadFormAsync();
            return View("Edit", opportunity);
        }

        opportunity.CreatedDate = DateTime.Now;
        Db.Opportunities.Add(opportunity);
        await Db.SaveChangesAsync();

        TempData["Success"] = "Opportunity created.";
        return RedirectToAction(nameof(Details), new { id = opportunity.Id });
    }

    public async Task<IActionResult> Edit(int id)
    {
        var opportunity = await Db.Opportunities.VisibleTo(User).FirstOrDefaultAsync(o => o.Id == id);
        if (opportunity == null) return NotFound();
        await LoadFormAsync();
        return View(opportunity);
    }

    [HttpPost, ActionName("Edit"), ValidateAntiForgeryToken]
    public async Task<IActionResult> EditPost(int id)
    {
        var opportunity = await Db.Opportunities.VisibleTo(User).FirstOrDefaultAsync(o => o.Id == id);
        if (opportunity == null) return NotFound();

        var fields = User.IsSalesOnly() ? EditableFields : EditableFields.Append(nameof(Opportunity.AssignedTo)).ToArray();
        await TryUpdateModelAsync(opportunity, "", m => fields.Contains(m.PropertyName));
        await ValidateRelatedAsync(opportunity.CustomerId, null);
        await ApplyAssignmentAsync(opportunity);
        if (!ModelState.IsValid)
        {
            await LoadFormAsync();
            return View(opportunity);
        }

        await Db.SaveChangesAsync();
        TempData["Success"] = "Opportunity updated.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = Roles.AdminOrManager)]
    public async Task<IActionResult> Delete(int id)
    {
        var opportunity = await Db.Opportunities.FindAsync(id);
        if (opportunity == null) return NotFound();
        Db.Opportunities.Remove(opportunity);
        await Db.SaveChangesAsync();
        TempData["Success"] = "Opportunity deleted.";
        return RedirectToAction(nameof(Index));
    }

    private async Task LoadFormAsync()
    {
        await LoadCustomersAndLeadsAsync();
        await LoadAssigneesAsync();
    }
}
