using AcxiomCRM.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Data;

/// <summary>Creates roles, one demo user per role and a little sample CRM data on first run.</summary>
public static class SeedData
{
    public static async Task InitializeAsync(IServiceProvider services)
    {
        var db = services.GetRequiredService<ApplicationDbContext>();
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = services.GetRequiredService<UserManager<IdentityUser>>();
        var config = services.GetRequiredService<IConfiguration>();

        await db.Database.MigrateAsync();

        foreach (var role in Roles.All)
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole(role));

        // Demo passwords come from configuration (appsettings.Development.json), not from code.
        var admin = await EnsureUserAsync(userManager, "admin@acxiom.local", config["SeedPasswords:Admin"], Roles.Admin);
        await EnsureUserAsync(userManager, "manager@acxiom.local", config["SeedPasswords:Manager"], Roles.Manager);
        var sales1 = await EnsureUserAsync(userManager, "sales1@acxiom.local", config["SeedPasswords:Sales"], Roles.SalesExecutive);
        var sales2 = await EnsureUserAsync(userManager, "sales2@acxiom.local", config["SeedPasswords:Sales"], Roles.SalesExecutive);

        if (await db.Customers.AnyAsync() || sales1 == null || sales2 == null) return;

        var now = DateTime.Now;
        var c1 = new Customer { CustomerCode = "CUST-0001", CustomerName = "Ravi Traders", Email = "contact@ravitraders.in", Phone = "9876543210", CompanyName = "Ravi Traders Pvt Ltd", City = "Pune", State = "Maharashtra", AssignedTo = sales1.Id, CreatedDate = now, CreatedBy = admin?.UserName };
        var c2 = new Customer { CustomerCode = "CUST-0002", CustomerName = "Meera Textiles", Email = "info@meeratextiles.in", Phone = "9123456780", CompanyName = "Meera Textiles", City = "Surat", State = "Gujarat", AssignedTo = sales1.Id, CreatedDate = now, CreatedBy = admin?.UserName };
        var c3 = new Customer { CustomerCode = "CUST-0003", CustomerName = "Kumar Logistics", Email = "ops@kumarlogistics.in", Phone = "8899776655", CompanyName = "Kumar Logistics", City = "Chennai", State = "Tamil Nadu", AssignedTo = sales2.Id, CreatedDate = now, CreatedBy = admin?.UserName };
        db.Customers.AddRange(c1, c2, c3);

        db.Leads.AddRange(
            new Lead { LeadCode = "LEAD-0001", LeadName = "Anita Sharma", Email = "anita@brightsoft.in", Phone = "9988776655", CompanyName = "BrightSoft", Source = LeadSource.Website, Status = LeadStatus.New, ExpectedValue = 150000, AssignedTo = sales1.Id, CreatedDate = now },
            new Lead { LeadCode = "LEAD-0002", LeadName = "Vikram Rao", Email = "vikram@rao-infra.in", Phone = "9765432109", CompanyName = "Rao Infra", Source = LeadSource.Referral, Status = LeadStatus.Contacted, Priority = LeadPriority.High, ExpectedValue = 400000, AssignedTo = sales1.Id, CreatedDate = now },
            new Lead { LeadCode = "LEAD-0003", LeadName = "Sneha Iyer", Email = "sneha@greenleaf.in", Phone = "9654321098", CompanyName = "GreenLeaf Foods", Source = LeadSource.Event, Status = LeadStatus.Qualified, ExpectedValue = 250000, AssignedTo = sales2.Id, CreatedDate = now },
            new Lead { LeadCode = "LEAD-0004", LeadName = "Arjun Mehta", CompanyName = "Mehta & Sons", Source = LeadSource.ColdCall, Status = LeadStatus.Lost, AssignedTo = sales2.Id, CreatedDate = now });

        db.Opportunities.AddRange(
            new Opportunity { OpportunityName = "Ravi ERP rollout", Customer = c1, Amount = 500000, Stage = OpportunityStage.Proposal, Probability = 50, ExpectedCloseDate = now.Date.AddDays(20), AssignedTo = sales1.Id, CreatedDate = now },
            new Opportunity { OpportunityName = "Meera annual contract", Customer = c2, Amount = 300000, Stage = OpportunityStage.Won, Probability = 100, ExpectedCloseDate = now.Date.AddMonths(-1), AssignedTo = sales1.Id, CreatedDate = now },
            new Opportunity { OpportunityName = "Kumar fleet tracking", Customer = c3, Amount = 750000, Stage = OpportunityStage.Negotiation, Probability = 70, ExpectedCloseDate = now.Date.AddDays(45), AssignedTo = sales2.Id, CreatedDate = now },
            new Opportunity { OpportunityName = "Kumar warehouse pilot", Customer = c3, Amount = 120000, Stage = OpportunityStage.Lost, Probability = 0, ExpectedCloseDate = now.Date.AddMonths(-2), AssignedTo = sales2.Id, CreatedDate = now });

        db.FollowUps.AddRange(
            new FollowUp { Customer = c1, FollowUpDate = now.Date.AddDays(2), FollowUpType = FollowUpType.Call, Remarks = "Discuss proposal pricing", AssignedTo = sales1.Id },
            new FollowUp { Customer = c3, FollowUpDate = now.Date.AddDays(5), FollowUpType = FollowUpType.Meeting, Remarks = "Negotiation meeting", AssignedTo = sales2.Id });

        db.Activities.Add(new Activity { ActivityType = ActivityType.Email, Subject = "Sent ERP brochure", Customer = c1, ActivityDate = now.Date, Status = ActivityStatus.Completed, AssignedTo = sales1.Id });

        await db.SaveChangesAsync();
    }

    private static async Task<IdentityUser?> EnsureUserAsync(UserManager<IdentityUser> userManager, string email, string? password, string role)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user != null) return user;
        if (string.IsNullOrEmpty(password)) return null;

        user = new IdentityUser { UserName = email, Email = email, EmailConfirmed = true };
        var result = await userManager.CreateAsync(user);
        if (result.Succeeded) result = await userManager.AddPasswordAsync(user, password);
        if (!result.Succeeded)
            throw new InvalidOperationException($"Could not seed {email}: {string.Join("; ", result.Errors.Select(e => e.Description))}");

        await userManager.AddToRoleAsync(user, role);
        return user;
    }
}
