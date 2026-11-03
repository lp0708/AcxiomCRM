using AcxiomCRM.Data;
using AcxiomCRM.Models;
using AcxiomCRM.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Controllers;

public class CustomersController : CrmControllerBase
{
    private static readonly string[] EditableFields =
        { nameof(Customer.CustomerName), nameof(Customer.Email), nameof(Customer.Phone), nameof(Customer.CompanyName),
          nameof(Customer.Address), nameof(Customer.City), nameof(Customer.State), nameof(Customer.Status), nameof(Customer.Notes) };

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
        return View("Edit", new Customer());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("CustomerName,Email,Phone,CompanyName,Address,City,State,Status,Notes,AssignedTo")] Customer customer)
    {
        await ValidateAsync(customer);
        if (!ModelState.IsValid)
        {
            await LoadAssigneesAsync();
            return View("Edit", customer);
        }

        customer.CustomerCode = NextCode("CUST", await Db.Customers.MaxAsync(c => (int?)c.Id) ?? 0);
        customer.CreatedDate = DateTime.Now;
        customer.CreatedBy = User.Identity?.Name;
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
        return View(customer);
    }

    [HttpPost, ActionName("Edit"), ValidateAntiForgeryToken]
    public async Task<IActionResult> EditPost(int id)
    {
        var customer = await Db.Customers.VisibleTo(User).FirstOrDefaultAsync(c => c.Id == id);
        if (customer == null) return NotFound();

        var fields = User.IsSalesOnly() ? EditableFields : EditableFields.Append(nameof(Customer.AssignedTo)).ToArray();
        await TryUpdateModelAsync(customer, "", m => fields.Contains(m.PropertyName));
        await ValidateAsync(customer);
        if (!ModelState.IsValid)
        {
            await LoadAssigneesAsync();
            return View(customer);
        }

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

    private async Task ValidateAsync(Customer customer)
    {
        customer.Email = customer.Email?.Trim().ToLowerInvariant() ?? "";
        customer.Phone = customer.Phone?.Trim() ?? "";
        await ApplyAssignmentAsync(customer);

        if (await Db.Customers.AnyAsync(c => c.Email == customer.Email && c.Id != customer.Id))
            ModelState.AddModelError(nameof(Customer.Email), "A customer with this email already exists.");
        if (await Db.Customers.AnyAsync(c => c.Phone == customer.Phone && c.Id != customer.Id))
            ModelState.AddModelError(nameof(Customer.Phone), "A customer with this phone number already exists.");
    }
}
