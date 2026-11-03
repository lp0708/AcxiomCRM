using AcxiomCRM.Data;
using AcxiomCRM.Models;
using AcxiomCRM.Services;
using AcxiomCRM.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Controllers;

public class OpportunitiesController : CrmControllerBase
{
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
        if (!string.IsNullOrEmpty(status)) query = query.Where(o => o.Status == status);

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
        return View("Edit", new OpportunityFormViewModel { CustomerId = customerId ?? 0 });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(OpportunityFormViewModel form)
    {
        form.Id = 0;
        await ValidateRelatedAsync(form.CustomerId, null);
        await ApplyAssignmentAsync(form);
        if (!ModelState.IsValid)
        {
            await LoadFormAsync();
            return View("Edit", form);
        }

        var opportunity = new Opportunity { CreatedDate = DateTime.Now };
        form.ApplyTo(opportunity);
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
        return View(OpportunityFormViewModel.From(opportunity));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, OpportunityFormViewModel form)
    {
        var opportunity = await Db.Opportunities.VisibleTo(User).FirstOrDefaultAsync(o => o.Id == id);
        if (opportunity == null) return NotFound();

        form.Id = id;
        await ValidateRelatedAsync(form.CustomerId, null);
        await ApplyAssignmentAsync(form);
        if (!ModelState.IsValid)
        {
            await LoadFormAsync();
            return View(form);
        }

        form.ApplyTo(opportunity);
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
