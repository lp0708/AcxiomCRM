namespace AcxiomCRM.Models;

public record ModulePermission(string Module, string Admin, string Manager, string SalesExecutive);

/// <summary>
/// The permission matrix shown on the Roles &amp; Permissions page. It documents what the
/// [Authorize] attributes and ScopeExtensions.VisibleTo() enforce; keep the two in sync.
/// </summary>
public static class Permissions
{
    public static readonly ModulePermission[] Matrix =
    {
        new("Dashboard", "All records", "All records", "Own records"),
        new("Customers", "Create, view, edit, delete", "Create, view, edit, delete", "Create, view, edit own"),
        new("Leads (incl. conversion)", "Create, view, edit, delete", "Create, view, edit, delete", "Create, view, edit own"),
        new("Opportunities", "Create, view, edit, delete", "Create, view, edit, delete", "Create, view, edit own"),
        new("Follow-Ups", "Create, view, edit, delete", "Create, view, edit, delete", "Create, view, edit own"),
        new("Activities", "Create, view, edit, delete", "Create, view, edit, delete", "Create, view, edit own"),
        new("Assign records to users", "Yes", "Yes", "No (always self)"),
        new("Pipeline Report", "Yes", "Yes", "No"),
        new("User Management", "Full", "No", "No"),
        new("Roles & Permissions", "View", "No", "No"),
        new("Audit Log", "View (read-only)", "No", "No"),
        new("REST API", "All records", "All records", "Own records; no delete"),
    };
}
