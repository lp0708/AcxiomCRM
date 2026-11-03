using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace AcxiomCRM.Models;

public class Activity : IOwned, IValidatableObject
{
    public int Id { get; set; }

    [Display(Name = "Type"), EnumDataType(typeof(ActivityType))]
    public ActivityType ActivityType { get; set; }

    [Required(ErrorMessage = "Subject is required."), StringLength(150)]
    public string Subject { get; set; } = "";

    [StringLength(1000)]
    public string? Description { get; set; }

    [Display(Name = "Date"), DataType(DataType.Date)]
    public DateTime ActivityDate { get; set; } = DateTime.Today;

    [Display(Name = "Customer")]
    public int? CustomerId { get; set; }
    public Customer? Customer { get; set; }

    [Display(Name = "Lead")]
    public int? LeadId { get; set; }
    public Lead? Lead { get; set; }

    [EnumDataType(typeof(ActivityStatus))]
    public ActivityStatus Status { get; set; } = ActivityStatus.Open;

    [Display(Name = "Assigned To")]
    public string? AssignedTo { get; set; }
    public IdentityUser? AssignedUser { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (CustomerId == null && LeadId == null)
            yield return new ValidationResult("Select a related customer or lead.", new[] { nameof(CustomerId) });
    }
}
