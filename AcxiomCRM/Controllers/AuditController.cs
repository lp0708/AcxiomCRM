using AcxiomCRM.Data;
using AcxiomCRM.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Controllers;

/// <summary>Read-only audit log viewer. There are deliberately no edit or delete actions.</summary>
[Authorize(Roles = Roles.Admin)]
public class AuditController : Controller
{
    private readonly ApplicationDbContext _db;

    public AuditController(ApplicationDbContext db)
    {
        _db = db;
    }

    // "eventAction" rather than "action": a parameter named action would bind to the route value ("Index").
    public async Task<IActionResult> Index(string? user, string? entity, string? eventAction, DateTime? from, DateTime? to)
    {
        var query = _db.AuditLogs.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(user)) query = query.Where(a => EF.Functions.Like(a.UserName ?? "", $"%{user.Trim()}%"));
        if (!string.IsNullOrEmpty(entity)) query = query.Where(a => a.EntityName == entity);
        if (!string.IsNullOrEmpty(eventAction)) query = query.Where(a => a.Action == eventAction);
        if (from != null) query = query.Where(a => a.CreatedDate >= from.Value.Date);
        if (to != null) query = query.Where(a => a.CreatedDate < to.Value.Date.AddDays(1));

        ViewBag.User = user;
        ViewBag.Entity = entity;
        ViewBag.EventAction = eventAction;
        ViewBag.From = from?.ToString("yyyy-MM-dd");
        ViewBag.To = to?.ToString("yyyy-MM-dd");
        ViewBag.Entities = await _db.AuditLogs.Select(a => a.EntityName).Distinct().OrderBy(e => e).ToListAsync();
        ViewBag.Actions = await _db.AuditLogs.Select(a => a.Action).Distinct().OrderBy(a => a).ToListAsync();

        return View(await query.OrderByDescending(a => a.Id).Take(500).ToListAsync());
    }
}
