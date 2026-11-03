using System.ComponentModel.DataAnnotations;
using AcxiomCRM.Models;
using AcxiomCRM.Models.Validation;

namespace AcxiomCRM.ViewModels;

public class OpportunityFormViewModel : IAssignable
{
    public int Id { get; set; }

    [Display(Name = "Opportunity Name")]
    [Required(ErrorMessage = "Opportunity Name is required."), StringLength(100)]
    public string OpportunityName { get; set; } = "";

    [Display(Name = "Customer")]
    [Range(1, int.MaxValue, ErrorMessage = "Customer is required.")]
    public int CustomerId { get; set; }

    [Range(typeof(decimal), "0.01", "1000000000", ErrorMessage = "Opportunity Amount must be greater than 0.")]
    public decimal Amount { get; set; }

    [EnumDataType(typeof(OpportunityStage), ErrorMessage = "Select a valid stage.")]
    public OpportunityStage Stage { get; set; } = OpportunityStage.Qualification;

    [Display(Name = "Probability (%)")]
    [Range(0, 100, ErrorMessage = "Probability must be between 0 and 100.")]
    public int Probability { get; set; }

    [Display(Name = "Expected Close Date"), DataType(DataType.Date)]
    [NotInPast(nameof(Stage), nameof(OpportunityStage.Won), nameof(OpportunityStage.Lost),
        ErrorMessage = "Expected Close Date cannot be in the past.")]
    public DateTime ExpectedCloseDate { get; set; } = DateTime.Today.AddDays(30);

    [StringLength(500)]
    public string? Notes { get; set; }

    [Display(Name = "Assigned To")]
    public string? AssignedTo { get; set; }

    public static OpportunityFormViewModel From(Opportunity o) => new()
    {
        Id = o.Id, OpportunityName = o.OpportunityName, CustomerId = o.CustomerId, Amount = o.Amount, Stage = o.Stage,
        Probability = o.Probability, ExpectedCloseDate = o.ExpectedCloseDate, Notes = o.Notes, AssignedTo = o.AssignedTo
    };

    public void ApplyTo(Opportunity o)
    {
        o.OpportunityName = OpportunityName.Trim();
        o.CustomerId = CustomerId;
        o.Amount = Amount;
        o.Stage = Stage;
        o.Probability = Probability;
        o.ExpectedCloseDate = ExpectedCloseDate.Date;
        o.Notes = Notes;
        o.AssignedTo = AssignedTo;
    }
}
