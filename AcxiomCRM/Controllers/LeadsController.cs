using AcxiomCRM.Data;
using AcxiomCRM.Models;
using AcxiomCRM.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Controllers;

public class LeadsController : CrmControllerBase
{
    private static readonly string[] EditableFields =
        { nameof(Lead.LeadName), nameof(Lead.Email), nameof(Lead.Phone), nameof(Lead.CompanyName), nameof(Lead.Source),
          nameof(Lead.Status), nameof(Lead.Priority), nameof(Lead.ExpectedValue), nameof(Lead.Notes) };

    public LeadsController(ApplicationDbContext db, UserManager<IdentityUser> users) : base(db, users) { }

    public async Task<IActionResult> Index(string? q, LeadStatus? status, string? assignedTo)
    {
        var query = Db.Leads.VisibleTo(User).Include(l => l.AssignedUser).AsQueryable();
        if (!string.IsNullOrWhiteSpace(q))
        {
            var like = $"%{q.Trim()}%";
            query = query.Where(l => EF.Functions.Like(l.LeadName, like) || EF.Functions.Like(l.CompanyName ?? "", like));
        }
        if (status != null) query = query.Where(l => l.Status == status);
        if (!string.IsNullOrEmpty(assignedTo)) query = query.Where(l => l.AssignedTo == assignedTo);

        ViewBag.Q = q;
        ViewBag.Status = status;
        ViewBag.AssignedTo = assignedTo;
        await LoadAssigneesAsync();
        return View(await query.OrderByDescending(l => l.CreatedDate).ToListAsync());
    }

    public async Task<IActionResult> Details(int id)
    {
        var lead = await Db.Leads.VisibleTo(User)
            .Include(l => l.AssignedUser)
            .Include(l => l.FollowUps)
            .Include(l => l.Activities)
            .FirstOrDefaultAsync(l => l.Id == id);
        return lead == null ? NotFound() : View(lead);
    }

    public async Task<IActionResult> Create()
    {
        await LoadFormAsync(null);
        return View("Edit", new Lead());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("LeadName,Email,Phone,CompanyName,Source,Status,Priority,ExpectedValue,Notes,AssignedTo")] Lead lead)
    {
        if (!LeadWorkflow.InitialStatuses.Contains(lead.Status))
            ModelState.AddModelError(nameof(Lead.Status), "A new lead cannot start with this status.");
        await ApplyAssignmentAsync(lead);
        if (!ModelState.IsValid)
        {
            await LoadFormAsync(null);
            return View("Edit", lead);
        }

        lead.LeadCode = NextCode("LEAD", await Db.Leads.MaxAsync(l => (int?)l.Id) ?? 0);
        lead.CreatedDate = DateTime.Now;
        Db.Leads.Add(lead);
        await Db.SaveChangesAsync();

        TempData["Success"] = "Lead created.";
        return RedirectToAction(nameof(Details), new { id = lead.Id });
    }

    public async Task<IActionResult> Edit(int id)
    {
        var lead = await Db.Leads.VisibleTo(User).FirstOrDefaultAsync(l => l.Id == id);
        if (lead == null) return NotFound();
        await LoadFormAsync(lead.Status);
        return View(lead);
    }

    [HttpPost, ActionName("Edit"), ValidateAntiForgeryToken]
    public async Task<IActionResult> EditPost(int id)
    {
        var lead = await Db.Leads.VisibleTo(User).FirstOrDefaultAsync(l => l.Id == id);
        if (lead == null) return NotFound();

        var currentStatus = lead.Status;
        var fields = User.IsSalesOnly() ? EditableFields : EditableFields.Append(nameof(Lead.AssignedTo)).ToArray();
        await TryUpdateModelAsync(lead, "", m => fields.Contains(m.PropertyName));

        if (!LeadWorkflow.CanMove(currentStatus, lead.Status))
            ModelState.AddModelError(nameof(Lead.Status), $"A lead cannot move from {currentStatus} to {lead.Status}.");
        await ApplyAssignmentAsync(lead);
        if (!ModelState.IsValid)
        {
            await LoadFormAsync(currentStatus);
            return View(lead);
        }

        await Db.SaveChangesAsync();
        TempData["Success"] = "Lead updated.";
        return RedirectToAction(nameof(Details), new { id });
    }

    /// <summary>Converts a Qualified lead into a customer (reusing one with the same email) and an opportunity.</summary>
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Convert(int id)
    {
        var lead = await Db.Leads.VisibleTo(User).FirstOrDefaultAsync(l => l.Id == id);
        if (lead == null) return NotFound();
        if (lead.Status != LeadStatus.Qualified)
        {
            TempData["Error"] = "Only Qualified leads can be converted.";
            return RedirectToAction(nameof(Details), new { id });
        }
        if (string.IsNullOrEmpty(lead.Email) || string.IsNullOrEmpty(lead.Phone))
        {
            TempData["Error"] = "Add the lead's email and phone before converting; both are required for a customer.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var email = lead.Email.ToLowerInvariant();
        var customer = await Db.Customers.FirstOrDefaultAsync(c => c.Email == email || c.Phone == lead.Phone);
        if (customer == null)
        {
            customer = new Customer
            {
                CustomerCode = NextCode("CUST", await Db.Customers.MaxAsync(c => (int?)c.Id) ?? 0),
                CustomerName = lead.LeadName,
                Email = email,
                Phone = lead.Phone,
                CompanyName = lead.CompanyName,
                Notes = $"Converted from lead {lead.LeadCode}",
                AssignedTo = lead.AssignedTo,
                CreatedDate = DateTime.Now,
                CreatedBy = User.Identity?.Name
            };
            Db.Customers.Add(customer);
        }

        if (lead.ExpectedValue > 0)
        {
            Db.Opportunities.Add(new Opportunity
            {
                OpportunityName = $"{lead.CompanyName ?? lead.LeadName} opportunity",
                Customer = customer,
                LeadId = lead.Id,
                Amount = lead.ExpectedValue,
                Probability = 10,
                ExpectedCloseDate = DateTime.Today.AddDays(30),
                AssignedTo = lead.AssignedTo,
                CreatedDate = DateTime.Now
            });
        }

        lead.Status = LeadStatus.Converted;
        await Db.SaveChangesAsync();

        TempData["Success"] = $"Lead converted to customer {customer.CustomerName}.";
        return RedirectToAction("Details", "Customers", new { id = customer.Id });
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = Roles.AdminOrManager)]
    public async Task<IActionResult> Delete(int id)
    {
        var lead = await Db.Leads.FindAsync(id);
        if (lead == null) return NotFound();
        Db.Leads.Remove(lead);
        await Db.SaveChangesAsync();
        TempData["Success"] = "Lead deleted.";
        return RedirectToAction(nameof(Index));
    }

    private async Task LoadFormAsync(LeadStatus? current)
    {
        ViewBag.Statuses = current == null ? LeadWorkflow.InitialStatuses : LeadWorkflow.AllowedFrom(current.Value).ToArray();
        await LoadAssigneesAsync();
    }
}
