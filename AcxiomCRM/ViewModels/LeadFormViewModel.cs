using System.ComponentModel.DataAnnotations;
using AcxiomCRM.Models;
using AcxiomCRM.Models.Validation;

namespace AcxiomCRM.ViewModels;

public class LeadFormViewModel : IAssignable
{
    public int Id { get; set; }

    [Display(Name = "Lead Name")]
    [Required(ErrorMessage = "Lead Name is required."), StringLength(100)]
    public string LeadName { get; set; } = "";

    [StringLength(150)]
    [RegularExpression(ValidationRules.Email, ErrorMessage = ValidationRules.EmailMessage)]
    public string? Email { get; set; }

    [RegularExpression(ValidationRules.Phone, ErrorMessage = ValidationRules.PhoneMessage)]
    public string? Phone { get; set; }

    [Display(Name = "Company"), StringLength(100)]
    public string? CompanyName { get; set; }

    [EnumDataType(typeof(LeadSource))]
    public LeadSource Source { get; set; }

    [Required(ErrorMessage = "Lead Status is required.")]
    [EnumDataType(typeof(LeadStatus), ErrorMessage = "Select a valid lead status.")]
    public LeadStatus Status { get; set; } = LeadStatus.New;

    [EnumDataType(typeof(LeadPriority))]
    public LeadPriority Priority { get; set; } = LeadPriority.Medium;

    [Display(Name = "Expected Value")]
    [Range(typeof(decimal), "0", "1000000000", ErrorMessage = "Expected Value must be between 0 and 1,000,000,000.")]
    public decimal ExpectedValue { get; set; }

    [StringLength(500)]
    public string? Notes { get; set; }

    [Display(Name = "Assigned To")]
    public string? AssignedTo { get; set; }

    public static LeadFormViewModel From(Lead l) => new()
    {
        Id = l.Id, LeadName = l.LeadName, Email = l.Email, Phone = l.Phone, CompanyName = l.CompanyName, Source = l.Source,
        Status = l.Status, Priority = l.Priority, ExpectedValue = l.ExpectedValue, Notes = l.Notes, AssignedTo = l.AssignedTo
    };

    public void ApplyTo(Lead l)
    {
        l.LeadName = LeadName.Trim();
        l.Email = Email?.Trim().ToLowerInvariant();
        l.Phone = Phone?.Trim();
        l.CompanyName = CompanyName;
        l.Source = Source;
        l.Status = Status;
        l.Priority = Priority;
        l.ExpectedValue = ExpectedValue;
        l.Notes = Notes;
        l.AssignedTo = AssignedTo;
    }
}
