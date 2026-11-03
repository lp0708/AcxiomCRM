using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using AcxiomCRM.Models.Validation;
using Microsoft.AspNetCore.Identity;

namespace AcxiomCRM.Models;

public class Opportunity : IOwned
{
    public int Id { get; set; }

    [Display(Name = "Opportunity Name")]
    [Required(ErrorMessage = "Opportunity Name is required."), StringLength(100)]
    public string OpportunityName { get; set; } = "";

    [Display(Name = "Customer")]
    [Range(1, int.MaxValue, ErrorMessage = "Customer is required.")]
    public int CustomerId { get; set; }
    public Customer? Customer { get; set; }

    [Display(Name = "Source Lead")]
    public int? LeadId { get; set; }
    public Lead? Lead { get; set; }

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

    [Display(Name = "Created")]
    public DateTime CreatedDate { get; set; }

    [Display(Name = "Assigned To")]
    public string? AssignedTo { get; set; }
    public IdentityUser? AssignedUser { get; set; }

    [NotMapped]
    public bool IsOpen => Stage is not (OpportunityStage.Won or OpportunityStage.Lost);

    /// <summary>Open / Won / Lost. Stored as a column (spec 17.3) but always derived from Stage, so it cannot drift.</summary>
    [StringLength(10)]
    public string Status
    {
        get => IsOpen ? "Open" : Stage.ToString();
        private set { }
    }

    [NotMapped, Display(Name = "Weighted Amount")]
    public decimal WeightedAmount => Amount * Probability / 100m;
}
