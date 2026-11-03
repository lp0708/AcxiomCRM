namespace AcxiomCRM.Models;

/// <summary>Anything with an assignee: CRM entities and the forms that edit them.</summary>
public interface IAssignable
{
    string? AssignedTo { get; set; }
}

/// <summary>
/// A CRM record assigned to a user. Sales Executives only see records assigned to them,
/// and every change to an IOwned entity is written to the audit log.
/// </summary>
public interface IOwned : IAssignable
{
    int Id { get; }
}
