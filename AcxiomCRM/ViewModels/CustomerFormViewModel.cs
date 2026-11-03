using System.ComponentModel.DataAnnotations;
using AcxiomCRM.Models;
using AcxiomCRM.Models.Validation;

namespace AcxiomCRM.ViewModels;

/// <summary>Create/Edit customer form. Only these fields can be posted; code, created date etc. stay server-side.</summary>
public class CustomerFormViewModel : IAssignable
{
    public int Id { get; set; }

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

    [Display(Name = "Assigned To")]
    public string? AssignedTo { get; set; }

    public static CustomerFormViewModel From(Customer c) => new()
    {
        Id = c.Id, CustomerName = c.CustomerName, Email = c.Email, Phone = c.Phone, CompanyName = c.CompanyName,
        Address = c.Address, City = c.City, State = c.State, Status = c.Status, Notes = c.Notes, AssignedTo = c.AssignedTo
    };

    public void ApplyTo(Customer c)
    {
        c.CustomerName = CustomerName.Trim();
        c.Email = Email;
        c.Phone = Phone;
        c.CompanyName = CompanyName;
        c.Address = Address;
        c.City = City;
        c.State = State;
        c.Status = Status;
        c.Notes = Notes;
        c.AssignedTo = AssignedTo;
    }
}
