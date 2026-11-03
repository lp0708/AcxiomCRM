using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using AcxiomCRM.Models.Validation;
using Microsoft.AspNetCore.Identity;

namespace AcxiomCRM.Models;

public class FollowUp : IOwned, IValidatableObject
{
    public int Id { get; set; }

    [Display(Name = "Customer")]
    public int? CustomerId { get; set; }
    public Customer? Customer { get; set; }

    [Display(Name = "Lead")]
    public int? LeadId { get; set; }
    public Lead? Lead { get; set; }

    [Display(Name = "Follow-Up Date"), DataType(DataType.Date)]
    [NotInPast(nameof(Status), nameof(FollowUpStatus.Completed), nameof(FollowUpStatus.Missed), nameof(FollowUpStatus.Cancelled),
        ErrorMessage = "Follow-up date cannot be earlier than today.")]
    public DateTime FollowUpDate { get; set; } = DateTime.Today;

    [Display(Name = "Type"), EnumDataType(typeof(FollowUpType))]
    public FollowUpType FollowUpType { get; set; }

    [StringLength(500)]
    public string? Remarks { get; set; }

    [EnumDataType(typeof(FollowUpStatus))]
    public FollowUpStatus Status { get; set; } = FollowUpStatus.Planned;

    [Display(Name = "Assigned To")]
    public string? AssignedTo { get; set; }
    public IdentityUser? AssignedUser { get; set; }

    [NotMapped]
    public bool IsOverdue => Status == FollowUpStatus.Planned && FollowUpDate.Date < DateTime.Today;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (CustomerId == null && LeadId == null)
            yield return new ValidationResult("Select a related customer or lead.", new[] { nameof(CustomerId) });
    }
}
