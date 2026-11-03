using System.Security.Claims;
using System.Text.Json;
using AcxiomCRM.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace AcxiomCRM.Data;

public class ApplicationDbContext : IdentityDbContext
{
    private readonly IHttpContextAccessor? _http;

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, IHttpContextAccessor? http = null)
        : base(options)
    {
        _http = http;
    }

    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Lead> Leads => Set<Lead>();
    public DbSet<Opportunity> Opportunities => Set<Opportunity>();
    public DbSet<FollowUp> FollowUps => Set<FollowUp>();
    public DbSet<Activity> Activities => Set<Activity>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Spec 17.3 names primary keys CustomerId, LeadId, ...; the C# property stays Id.
        builder.Entity<Customer>().Property(c => c.Id).HasColumnName("CustomerId");
        builder.Entity<Lead>().Property(l => l.Id).HasColumnName("LeadId");
        builder.Entity<Opportunity>().Property(o => o.Id).HasColumnName("OpportunityId");
        builder.Entity<FollowUp>().Property(f => f.Id).HasColumnName("FollowUpId");
        builder.Entity<Activity>().Property(a => a.Id).HasColumnName("ActivityId");
        builder.Entity<AuditLog>().Property(a => a.Id).HasColumnName("AuditLogId");

        builder.Entity<Customer>(e =>
        {
            e.HasIndex(c => c.Email).IsUnique();
            e.HasIndex(c => c.Phone).IsUnique();
            e.HasIndex(c => c.CustomerCode).IsUnique();
            e.HasOne(c => c.AssignedUser).WithMany().HasForeignKey(c => c.AssignedTo).OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<Lead>(e =>
        {
            e.HasIndex(l => l.LeadCode).IsUnique();
            // SQLite cannot aggregate decimals, so money is stored as REAL.
            e.Property(l => l.ExpectedValue).HasConversion<double>();
            e.HasOne(l => l.AssignedUser).WithMany().HasForeignKey(l => l.AssignedTo).OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<Opportunity>(e =>
        {
            e.Property(o => o.Amount).HasConversion<double>();
            e.HasIndex(o => o.Status);
            e.HasOne(o => o.Customer).WithMany(c => c.Opportunities).HasForeignKey(o => o.CustomerId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(o => o.Lead).WithMany().HasForeignKey(o => o.LeadId).OnDelete(DeleteBehavior.SetNull);
            e.HasOne(o => o.AssignedUser).WithMany().HasForeignKey(o => o.AssignedTo).OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<FollowUp>(e =>
        {
            e.HasOne(f => f.Customer).WithMany(c => c.FollowUps).HasForeignKey(f => f.CustomerId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(f => f.Lead).WithMany(l => l.FollowUps).HasForeignKey(f => f.LeadId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(f => f.AssignedUser).WithMany().HasForeignKey(f => f.AssignedTo).OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<Activity>(e =>
        {
            e.HasOne(a => a.Customer).WithMany(c => c.Activities).HasForeignKey(a => a.CustomerId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(a => a.Lead).WithMany(l => l.Activities).HasForeignKey(a => a.LeadId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(a => a.AssignedUser).WithMany().HasForeignKey(a => a.AssignedTo).OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<AuditLog>(e =>
        {
            e.HasIndex(a => a.CreatedDate);
            e.HasIndex(a => new { a.EntityName, a.RecordId });
        });
    }

    /// <summary>Adds an audit entry for a security event (login, role change, ...). Caller saves.</summary>
    public void AddAudit(string action, string entityName, string? recordId,
        string? oldValue = null, string? newValue = null, string? userId = null, string? userName = null)
    {
        var user = _http?.HttpContext?.User;
        AuditLogs.Add(new AuditLog
        {
            Action = action,
            EntityName = entityName,
            RecordId = recordId,
            OldValue = oldValue,
            NewValue = newValue,
            UserId = userId ?? user?.FindFirstValue(ClaimTypes.NameIdentifier),
            UserName = userName ?? user?.Identity?.Name ?? "System",
            IpAddress = _http?.HttpContext?.Connection.RemoteIpAddress?.ToString(),
            CreatedDate = DateTime.Now
        });
    }

    /// <summary>Saves changes and writes a Create/Update/Delete audit entry for every changed CRM record.</summary>
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ChangeTracker.DetectChanges();
        var pending = ChangeTracker.Entries<IOwned>()
            .Where(e => e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            .Select(e => new
            {
                Entry = e,
                Action = e.State switch { EntityState.Added => "Create", EntityState.Modified => "Update", _ => "Delete" },
                RecordId = e.State == EntityState.Added ? null : e.Entity.Id.ToString(),
                OldValue = e.State switch
                {
                    EntityState.Modified => Serialize(e, original: true, onlyModified: true),
                    EntityState.Deleted => Serialize(e, original: true, onlyModified: false),
                    _ => null
                },
                NewValue = e.State == EntityState.Modified ? Serialize(e, original: false, onlyModified: true) : null
            })
            .ToList();

        var result = await base.SaveChangesAsync(cancellationToken);

        if (pending.Count > 0)
        {
            foreach (var p in pending)
            {
                AddAudit(p.Action, p.Entry.Metadata.ClrType.Name,
                    p.RecordId ?? p.Entry.Entity.Id.ToString(),
                    p.OldValue,
                    p.Action == "Create" ? Serialize(p.Entry, original: false, onlyModified: false) : p.NewValue);
            }
            await base.SaveChangesAsync(cancellationToken);
        }

        return result;
    }

    private static string Serialize(EntityEntry entry, bool original, bool onlyModified) =>
        JsonSerializer.Serialize(entry.Properties
            .Where(p => !onlyModified || p.IsModified)
            .ToDictionary(p => p.Metadata.Name, p => original ? p.OriginalValue : p.CurrentValue));
}
