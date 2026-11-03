using AcxiomCRM.Data;
using AcxiomCRM.Models;
using AcxiomCRM.Models.Dtos;
using AcxiomCRM.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Controllers.Api;

[ApiController]
[Route("api/customers")]
[Produces("application/json")]
public class CustomersApiController : CrmControllerBase
{
    public CustomersApiController(ApplicationDbContext db, UserManager<IdentityUser> users) : base(db, users) { }

    /// <summary>GET /api/customers?q= — customers visible to the caller.</summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<CustomerDto>>> GetAll(string? q)
    {
        var query = Db.Customers.VisibleTo(User).Include(c => c.AssignedUser).AsNoTracking();
        if (!string.IsNullOrWhiteSpace(q))
        {
            var like = $"%{q.Trim()}%";
            query = query.Where(c => EF.Functions.Like(c.CustomerName, like) || EF.Functions.Like(c.Email, like));
        }
        var customers = await query.OrderBy(c => c.CustomerName).ToListAsync();
        return customers.Select(CustomerDto.From).ToList();
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<CustomerDto>> Get(int id)
    {
        var customer = await Db.Customers.VisibleTo(User).Include(c => c.AssignedUser).AsNoTracking().FirstOrDefaultAsync(c => c.Id == id);
        return customer == null ? NotFound() : CustomerDto.From(customer);
    }

    [HttpPost]
    public async Task<ActionResult<CustomerDto>> Create(CustomerInputDto input)
    {
        var customer = new Customer { AssignedTo = input.AssignedTo };
        input.ApplyTo(customer);
        await ApplyAssignmentAsync(customer);
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        if (await IsDuplicateAsync(customer)) return Conflict(new { error = "A customer with this email or phone already exists." });

        customer.CustomerCode = NextCode("CUST", await Db.Customers.MaxAsync(c => (int?)c.Id) ?? 0);
        customer.CreatedDate = DateTime.Now;
        customer.CreatedBy = User.Identity?.Name;
        Db.Customers.Add(customer);
        await Db.SaveChangesAsync();
        await Db.Entry(customer).Reference(c => c.AssignedUser).LoadAsync();

        return CreatedAtAction(nameof(Get), new { id = customer.Id }, CustomerDto.From(customer));
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<CustomerDto>> Update(int id, CustomerInputDto input)
    {
        var customer = await Db.Customers.VisibleTo(User).Include(c => c.AssignedUser).FirstOrDefaultAsync(c => c.Id == id);
        if (customer == null) return NotFound();

        input.ApplyTo(customer);
        if (!User.IsSalesOnly()) customer.AssignedTo = input.AssignedTo;
        await ApplyAssignmentAsync(customer);
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        if (await IsDuplicateAsync(customer)) return Conflict(new { error = "A customer with this email or phone already exists." });

        await Db.SaveChangesAsync();
        return CustomerDto.From(customer);
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = Roles.AdminOrManager)]
    public async Task<IActionResult> Delete(int id)
    {
        var customer = await Db.Customers.Include(c => c.Opportunities).FirstOrDefaultAsync(c => c.Id == id);
        if (customer == null) return NotFound();
        if (customer.Opportunities.Count > 0)
            return Conflict(new { error = "Customer has opportunities; set Status to Inactive instead." });

        Db.Customers.Remove(customer);
        await Db.SaveChangesAsync();
        return NoContent();
    }

    private Task<bool> IsDuplicateAsync(Customer customer) =>
        Db.Customers.AnyAsync(c => c.Id != customer.Id && (c.Email == customer.Email || c.Phone == customer.Phone));
}
