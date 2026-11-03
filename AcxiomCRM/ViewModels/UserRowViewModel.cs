namespace AcxiomCRM.ViewModels;

/// <summary>A user as shown on the admin Users page. Never carries password hashes or security stamps.</summary>
public class UserRowViewModel
{
    public string Id { get; set; } = "";
    public string? UserName { get; set; }
    public string? Role { get; set; }
    public bool IsActive { get; set; }
    public bool IsLockedOut { get; set; }
    public int FailedLoginCount { get; set; }
}
