using System.ComponentModel.DataAnnotations;
using AcxiomCRM.Models;

namespace AcxiomCRM.ViewModels;

public class ActivityFormViewModel : IAssignable, IValidatableObject
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

    [Display(Name = "Lead")]
    public int? LeadId { get; set; }

    [EnumDataType(typeof(ActivityStatus))]
    public ActivityStatus Status { get; set; } = ActivityStatus.Open;

    [Display(Name = "Assigned To")]
    public string? AssignedTo { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (CustomerId == null && LeadId == null)
            yield return new ValidationResult("Select a related customer or lead.", new[] { nameof(CustomerId) });
    }

    public static ActivityFormViewModel From(Activity a) => new()
    {
        Id = a.Id, ActivityType = a.ActivityType, Subject = a.Subject, Description = a.Description, ActivityDate = a.ActivityDate,
        CustomerId = a.CustomerId, LeadId = a.LeadId, Status = a.Status, AssignedTo = a.AssignedTo
    };

    public void ApplyTo(Activity a)
    {
        a.ActivityType = ActivityType;
        a.Subject = Subject.Trim();
        a.Description = Description;
        a.ActivityDate = ActivityDate.Date;
        a.CustomerId = CustomerId;
        a.LeadId = LeadId;
        a.Status = Status;
        a.AssignedTo = AssignedTo;
    }
}
