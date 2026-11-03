using AcxiomCRM.Data;
using AcxiomCRM.Models;
using AcxiomCRM.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Controllers;

[Authorize(Roles = Roles.AdminOrManager)]
public class ReportsController : Controller
{
    private readonly ApplicationDbContext _db;

    public ReportsController(ApplicationDbContext db)
    {
        _db = db;
    }

    /// <summary>Open pipeline grouped by stage and by owner, with weighted amount (Amount × Probability / 100).</summary>
    public async Task<IActionResult> Pipeline()
    {
        var open = await _db.Opportunities.Include(o => o.AssignedUser)
            .Where(o => o.Stage != OpportunityStage.Won && o.Stage != OpportunityStage.Lost)
            .ToListAsync();

        PipelineRow Row(string key, IEnumerable<Opportunity> items) =>
            new(key, items.Count(), items.Sum(o => o.Amount), items.Sum(o => o.WeightedAmount));

        var model = new PipelineReportViewModel
        {
            ByStage = open.GroupBy(o => o.Stage).OrderBy(g => g.Key).Select(g => Row(g.Key.ToString(), g)).ToList(),
            ByOwner = open.GroupBy(o => o.AssignedUser?.UserName ?? "Unassigned").OrderBy(g => g.Key).Select(g => Row(g.Key, g)).ToList(),
            Total = Row("Total", open)
        };
        return View(model);
    }
}
