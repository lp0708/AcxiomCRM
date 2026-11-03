using AcxiomCRM.Data;
using AcxiomCRM.Models;
using AcxiomCRM.Models.Dtos;
using AcxiomCRM.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Controllers.Api;

[ApiController]
[Route("api/opportunities")]
[Produces("application/json")]
public class OpportunitiesApiController : CrmControllerBase
{
    public OpportunitiesApiController(ApplicationDbContext db, UserManager<IdentityUser> users) : base(db, users) { }

    /// <summary>GET /api/opportunities?stage= — opportunities visible to the caller.</summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<OpportunityDto>>> GetAll(OpportunityStage? stage)
    {
        var query = Db.Opportunities.VisibleTo(User).Include(o => o.Customer).Include(o => o.AssignedUser).AsNoTracking();
        if (stage != null) query = query.Where(o => o.Stage == stage);
        var opportunities = await query.OrderBy(o => o.ExpectedCloseDate).ToListAsync();
        return opportunities.Select(OpportunityDto.From).ToList();
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<OpportunityDto>> Get(int id)
    {
        var opportunity = await Db.Opportunities.VisibleTo(User).Include(o => o.Customer).Include(o => o.AssignedUser)
            .AsNoTracking().FirstOrDefaultAsync(o => o.Id == id);
        return opportunity == null ? NotFound() : OpportunityDto.From(opportunity);
    }

    [HttpPost]
    public async Task<ActionResult<OpportunityDto>> Create(OpportunityInputDto input)
    {
        var opportunity = input.ToEntity();
        await ValidateRelatedAsync(opportunity.CustomerId, null);
        await ApplyAssignmentAsync(opportunity);
        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        opportunity.CreatedDate = DateTime.Now;
        Db.Opportunities.Add(opportunity);
        await Db.SaveChangesAsync();
        await Db.Entry(opportunity).Reference(o => o.Customer).LoadAsync();
        await Db.Entry(opportunity).Reference(o => o.AssignedUser).LoadAsync();

        return CreatedAtAction(nameof(Get), new { id = opportunity.Id }, OpportunityDto.From(opportunity));
    }
}
