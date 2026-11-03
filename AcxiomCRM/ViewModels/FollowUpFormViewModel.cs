using System.ComponentModel.DataAnnotations;
using AcxiomCRM.Models;
using AcxiomCRM.Models.Validation;

namespace AcxiomCRM.ViewModels;

public class FollowUpFormViewModel : IAssignable, IValidatableObject
{
    public int Id { get; set; }

    [Display(Name = "Customer")]
    public int? CustomerId { get; set; }

    [Display(Name = "Lead")]
    public int? LeadId { get; set; }

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

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (CustomerId == null && LeadId == null)
            yield return new ValidationResult("Select a related customer or lead.", new[] { nameof(CustomerId) });
    }

    public static FollowUpFormViewModel From(FollowUp f) => new()
    {
        Id = f.Id, CustomerId = f.CustomerId, LeadId = f.LeadId, FollowUpDate = f.FollowUpDate, FollowUpType = f.FollowUpType,
        Remarks = f.Remarks, Status = f.Status, AssignedTo = f.AssignedTo
    };

    public void ApplyTo(FollowUp f)
    {
        f.CustomerId = CustomerId;
        f.LeadId = LeadId;
        f.FollowUpDate = FollowUpDate.Date;
        f.FollowUpType = FollowUpType;
        f.Remarks = Remarks;
        f.Status = Status;
        f.AssignedTo = AssignedTo;
    }
}
