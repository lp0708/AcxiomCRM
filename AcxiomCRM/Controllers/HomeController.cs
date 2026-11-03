using AcxiomCRM.Data;
using AcxiomCRM.Models;
using AcxiomCRM.Services;
using AcxiomCRM.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Controllers;

public class HomeController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly int _monthlySalesMonths;

    public HomeController(ApplicationDbContext db, IConfiguration config)
    {
        _db = db;
        // Reporting period of the Monthly Sales chart, configured in appsettings.json.
        _monthlySalesMonths = Math.Clamp(config.GetValue("Dashboard:MonthlySalesMonths", 6), 1, 24);
    }

    /// <summary>Dashboard. Every number is computed from records the current user is allowed to see.</summary>
    public async Task<IActionResult> Index()
    {
        var leadStatuses = await _db.Leads.VisibleTo(User).Select(l => l.Status).ToListAsync();
        var opportunities = await _db.Opportunities.VisibleTo(User)
            .Select(o => new { o.Stage, o.Amount, o.ExpectedCloseDate })
            .ToListAsync();
        bool IsOpen(OpportunityStage s) => s is not (OpportunityStage.Won or OpportunityStage.Lost);

        var firstMonth = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1).AddMonths(1 - _monthlySalesMonths);
        var won = opportunities.Where(o => o.Stage == OpportunityStage.Won).ToList();

        var model = new DashboardViewModel
        {
            TotalCustomers = await _db.Customers.VisibleTo(User).CountAsync(),
            TotalLeads = leadStatuses.Count,
            OpenLeads = leadStatuses.Count(LeadWorkflow.IsOpen),
            TotalOpportunities = opportunities.Count,
            OpenOpportunities = opportunities.Count(o => IsOpen(o.Stage)),
            WonOpportunities = won.Count,
            LostOpportunities = opportunities.Count(o => o.Stage == OpportunityStage.Lost),
            TotalPipelineValue = opportunities.Where(o => IsOpen(o.Stage)).Sum(o => o.Amount),
            PendingFollowUps = await _db.FollowUps.VisibleTo(User).CountAsync(f => f.Status == FollowUpStatus.Planned),

            LeadStatusChart = new[] { LeadStatus.New, LeadStatus.Contacted, LeadStatus.Qualified, LeadStatus.Lost, LeadStatus.Converted }
                .ToDictionary(s => s.ToString(), s => leadStatuses.Count(x => x == s)),
            PipelineChart = Enum.GetValues<OpportunityStage>()
                .ToDictionary(s => s.ToString(), s => opportunities.Count(o => o.Stage == s)),
            MonthlySalesMonths = _monthlySalesMonths,
            MonthlySalesChart = Enumerable.Range(0, _monthlySalesMonths)
                .Select(i => firstMonth.AddMonths(i))
                .ToDictionary(m => m.ToString("MMM yyyy"),
                    m => won.Where(o => o.ExpectedCloseDate.Year == m.Year && o.ExpectedCloseDate.Month == m.Month).Sum(o => o.Amount))
        };

        return View(model);
    }

    [AllowAnonymous]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = System.Diagnostics.Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
