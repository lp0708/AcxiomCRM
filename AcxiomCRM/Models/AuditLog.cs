namespace AcxiomCRM.Models;

/// <summary>Append-only record of security and business events. Never edited or deleted by the app.</summary>
public class AuditLog
{
    public int Id { get; set; }
    public string? UserId { get; set; }
    public string? UserName { get; set; }
    public string Action { get; set; } = "";
    public string EntityName { get; set; } = "";
    public string? RecordId { get; set; }
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    public DateTime CreatedDate { get; set; }
    public string? IpAddress { get; set; }
}
