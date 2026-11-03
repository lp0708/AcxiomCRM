namespace AcxiomCRM.Models;

/// <summary>
/// A CRM record assigned to a user. Sales Executives only see records assigned to them,
/// and every change to an IOwned entity is written to the audit log.
/// </summary>
public interface IOwned
{
    int Id { get; }
    string? AssignedTo { get; set; }
}
