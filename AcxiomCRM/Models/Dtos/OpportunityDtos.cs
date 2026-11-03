using System.ComponentModel.DataAnnotations;
using AcxiomCRM.Models.Validation;

namespace AcxiomCRM.Models.Dtos;

public record OpportunityDto(int Id, string OpportunityName, int CustomerId, string? CustomerName, decimal Amount,
    OpportunityStage Stage, string Status, int Probability, decimal WeightedAmount, DateTime ExpectedCloseDate,
    string? AssignedTo, DateTime CreatedDate)
{
    public static OpportunityDto From(Opportunity o) => new(o.Id, o.OpportunityName, o.CustomerId, o.Customer?.CustomerName,
        o.Amount, o.Stage, o.Status, o.Probability, o.WeightedAmount, o.ExpectedCloseDate, o.AssignedUser?.UserName, o.CreatedDate);
}

public class OpportunityInputDto
{
    [Required, StringLength(100)]
    public string OpportunityName { get; set; } = "";

    [Range(1, int.MaxValue, ErrorMessage = "Customer is required.")]
    public int CustomerId { get; set; }

    [Range(typeof(decimal), "0.01", "1000000000", ErrorMessage = "Opportunity Amount must be greater than 0.")]
    public decimal Amount { get; set; }

    [EnumDataType(typeof(OpportunityStage))]
    public OpportunityStage Stage { get; set; } = OpportunityStage.Qualification;

    [Range(0, 100, ErrorMessage = "Probability must be between 0 and 100.")]
    public int Probability { get; set; }

    [NotInPast(nameof(Stage), nameof(OpportunityStage.Won), nameof(OpportunityStage.Lost),
        ErrorMessage = "Expected Close Date cannot be in the past.")]
    public DateTime ExpectedCloseDate { get; set; }

    [StringLength(500)] public string? Notes { get; set; }
    public string? AssignedTo { get; set; }

    public Opportunity ToEntity() => new()
    {
        OpportunityName = OpportunityName, CustomerId = CustomerId, Amount = Amount, Stage = Stage, Probability = Probability,
        ExpectedCloseDate = ExpectedCloseDate.Date, Notes = Notes, AssignedTo = AssignedTo
    };
}
