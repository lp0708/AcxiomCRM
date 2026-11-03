using System.ComponentModel.DataAnnotations;
using AcxiomCRM.Models.Validation;

namespace AcxiomCRM.Models.Dtos;

public record LeadDto(int Id, string? LeadCode, string LeadName, string? Email, string? Phone, string? CompanyName,
    LeadSource Source, LeadStatus Status, LeadPriority Priority, decimal ExpectedValue, string? AssignedTo, DateTime CreatedDate)
{
    public static LeadDto From(Lead l) => new(l.Id, l.LeadCode, l.LeadName, l.Email, l.Phone, l.CompanyName,
        l.Source, l.Status, l.Priority, l.ExpectedValue, l.AssignedUser?.UserName, l.CreatedDate);
}

public class LeadInputDto
{
    [Required, StringLength(100)]
    public string LeadName { get; set; } = "";

    [StringLength(150), RegularExpression(ValidationRules.Email, ErrorMessage = ValidationRules.EmailMessage)]
    public string? Email { get; set; }

    [RegularExpression(ValidationRules.Phone, ErrorMessage = ValidationRules.PhoneMessage)]
    public string? Phone { get; set; }

    [StringLength(100)] public string? CompanyName { get; set; }
    [EnumDataType(typeof(LeadSource))] public LeadSource Source { get; set; }
    [EnumDataType(typeof(LeadStatus))] public LeadStatus Status { get; set; } = LeadStatus.New;
    [EnumDataType(typeof(LeadPriority))] public LeadPriority Priority { get; set; } = LeadPriority.Medium;

    [Range(typeof(decimal), "0", "1000000000", ErrorMessage = "Expected Value must be between 0 and 1,000,000,000.")]
    public decimal ExpectedValue { get; set; }

    [StringLength(500)] public string? Notes { get; set; }
    public string? AssignedTo { get; set; }

    public Lead ToEntity() => new()
    {
        LeadName = LeadName, Email = Email?.Trim().ToLowerInvariant(), Phone = Phone?.Trim(), CompanyName = CompanyName,
        Source = Source, Status = Status, Priority = Priority, ExpectedValue = ExpectedValue, Notes = Notes, AssignedTo = AssignedTo
    };
}
