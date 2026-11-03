using System.ComponentModel.DataAnnotations;
using AcxiomCRM.Models.Validation;
using Microsoft.AspNetCore.Identity;

namespace AcxiomCRM.Models;

public class Customer : IOwned
{
    public int Id { get; set; }

    [Display(Name = "Code"), StringLength(20)]
    public string? CustomerCode { get; set; }

    [Display(Name = "Customer Name")]
    [Required(ErrorMessage = "Customer Name is required."), StringLength(100)]
    public string CustomerName { get; set; } = "";

    [Required(ErrorMessage = "Email is required."), StringLength(150)]
    [RegularExpression(ValidationRules.Email, ErrorMessage = ValidationRules.EmailMessage)]
    public string Email { get; set; } = "";

    [Required(ErrorMessage = "Phone is required.")]
    [RegularExpression(ValidationRules.Phone, ErrorMessage = ValidationRules.PhoneMessage)]
    public string Phone { get; set; } = "";

    [Display(Name = "Company"), StringLength(100)]
    public string? CompanyName { get; set; }

    [StringLength(250)]
    public string? Address { get; set; }

    [StringLength(60)]
    public string? City { get; set; }

    [StringLength(60)]
    public string? State { get; set; }

    [EnumDataType(typeof(CustomerStatus))]
    public CustomerStatus Status { get; set; } = CustomerStatus.Active;

    [StringLength(500)]
    public string? Notes { get; set; }

    [Display(Name = "Created")]
    public DateTime CreatedDate { get; set; }

    [Display(Name = "Created By")]
    public string? CreatedBy { get; set; }

    [Display(Name = "Assigned To")]
    public string? AssignedTo { get; set; }
    public IdentityUser? AssignedUser { get; set; }

    public List<Opportunity> Opportunities { get; set; } = new();
    public List<FollowUp> FollowUps { get; set; } = new();
    public List<Activity> Activities { get; set; } = new();
}
