using AcxiomCRM.Data;
using AcxiomCRM.Models;
using AcxiomCRM.Models.Dtos;
using AcxiomCRM.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Controllers.Api;

[ApiController]
[Route("api/leads")]
[Produces("application/json")]
public class LeadsApiController : CrmControllerBase
{
    public LeadsApiController(ApplicationDbContext db, UserManager<IdentityUser> users) : base(db, users) { }

    /// <summary>GET /api/leads?status= — leads visible to the caller.</summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<LeadDto>>> GetAll(LeadStatus? status)
    {
        var query = Db.Leads.VisibleTo(User).Include(l => l.AssignedUser).AsNoTracking();
        if (status != null) query = query.Where(l => l.Status == status);
        var leads = await query.OrderByDescending(l => l.CreatedDate).ToListAsync();
        return leads.Select(LeadDto.From).ToList();
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<LeadDto>> Get(int id)
    {
        var lead = await Db.Leads.VisibleTo(User).Include(l => l.AssignedUser).AsNoTracking().FirstOrDefaultAsync(l => l.Id == id);
        return lead == null ? NotFound() : LeadDto.From(lead);
    }

    [HttpPost]
    public async Task<ActionResult<LeadDto>> Create(LeadInputDto input)
    {
        var lead = input.ToEntity();
        if (!LeadWorkflow.InitialStatuses.Contains(lead.Status))
            ModelState.AddModelError(nameof(Lead.Status), "A new lead cannot start with this status.");
        await ApplyAssignmentAsync(lead);
        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        lead.LeadCode = NextCode("LEAD", await Db.Leads.MaxAsync(l => (int?)l.Id) ?? 0);
        lead.CreatedDate = DateTime.Now;
        Db.Leads.Add(lead);
        await Db.SaveChangesAsync();
        await Db.Entry(lead).Reference(l => l.AssignedUser).LoadAsync();

        return CreatedAtAction(nameof(Get), new { id = lead.Id }, LeadDto.From(lead));
    }
}
