using AcxiomCRM.Data;
using AcxiomCRM.Models;
using AcxiomCRM.Services;
using AcxiomCRM.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Controllers;

public class CustomersController : CrmControllerBase
{
    public CustomersController(ApplicationDbContext db, UserManager<IdentityUser> users) : base(db, users) { }

    public async Task<IActionResult> Index(string? q, CustomerStatus? status)
    {
        var query = Db.Customers.VisibleTo(User).Include(c => c.AssignedUser).AsQueryable();
        if (!string.IsNullOrWhiteSpace(q))
        {
            var like = $"%{q.Trim()}%";
            query = query.Where(c => EF.Functions.Like(c.CustomerName, like) || EF.Functions.Like(c.Email, like)
                || EF.Functions.Like(c.Phone, like) || EF.Functions.Like(c.CompanyName ?? "", like));
        }
        if (status != null) query = query.Where(c => c.Status == status);

        ViewBag.Q = q;
        ViewBag.Status = status;
        return View(await query.OrderBy(c => c.CustomerName).ToListAsync());
    }

    public async Task<IActionResult> Details(int id)
    {
        var customer = await Db.Customers.VisibleTo(User)
            .Include(c => c.AssignedUser)
            .Include(c => c.Opportunities)
            .Include(c => c.FollowUps)
            .Include(c => c.Activities)
            .FirstOrDefaultAsync(c => c.Id == id);
        return customer == null ? NotFound() : View(customer);
    }

    public async Task<IActionResult> Create()
    {
        await LoadAssigneesAsync();
        return View("Edit", new CustomerFormViewModel());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CustomerFormViewModel form)
    {
        form.Id = 0;
        await ValidateAsync(form);
        if (!ModelState.IsValid)
        {
            await LoadAssigneesAsync();
            return View("Edit", form);
        }

        var customer = new Customer
        {
            CustomerCode = NextCode("CUST", await Db.Customers.MaxAsync(c => (int?)c.Id) ?? 0),
            CreatedDate = DateTime.Now,
            CreatedBy = User.Identity?.Name
        };
        form.ApplyTo(customer);
        Db.Customers.Add(customer);
        await Db.SaveChangesAsync();

        TempData["Success"] = "Customer created.";
        return RedirectToAction(nameof(Details), new { id = customer.Id });
    }

    public async Task<IActionResult> Edit(int id)
    {
        var customer = await Db.Customers.VisibleTo(User).FirstOrDefaultAsync(c => c.Id == id);
        if (customer == null) return NotFound();
        await LoadAssigneesAsync();
        return View(CustomerFormViewModel.From(customer));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, CustomerFormViewModel form)
    {
        var customer = await Db.Customers.VisibleTo(User).FirstOrDefaultAsync(c => c.Id == id);
        if (customer == null) return NotFound();

        form.Id = id;
        await ValidateAsync(form);
        if (!ModelState.IsValid)
        {
            await LoadAssigneesAsync();
            return View(form);
        }

        form.ApplyTo(customer);
        await Db.SaveChangesAsync();
        TempData["Success"] = "Customer updated.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = Roles.AdminOrManager)]
    public async Task<IActionResult> Delete(int id)
    {
        var customer = await Db.Customers.Include(c => c.Opportunities).FirstOrDefaultAsync(c => c.Id == id);
        if (customer == null) return NotFound();
        if (customer.Opportunities.Count > 0)
        {
            TempData["Error"] = "This customer has opportunities. Set the customer to Inactive instead of deleting.";
            return RedirectToAction(nameof(Details), new { id });
        }

        Db.Customers.Remove(customer);
        await Db.SaveChangesAsync();
        TempData["Success"] = "Customer deleted.";
        return RedirectToAction(nameof(Index));
    }

    private async Task ValidateAsync(CustomerFormViewModel form)
    {
        form.Email = form.Email?.Trim().ToLowerInvariant() ?? "";
        form.Phone = form.Phone?.Trim() ?? "";
        await ApplyAssignmentAsync(form);

        if (await Db.Customers.AnyAsync(c => c.Email == form.Email && c.Id != form.Id))
            ModelState.AddModelError(nameof(form.Email), "A customer with this email already exists.");
        if (await Db.Customers.AnyAsync(c => c.Phone == form.Phone && c.Id != form.Id))
            ModelState.AddModelError(nameof(form.Phone), "A customer with this phone number already exists.");
    }
}
