using System.ComponentModel.DataAnnotations;
using AcxiomCRM.Models.Validation;

namespace AcxiomCRM.Models.Dtos;

public record CustomerDto(int Id, string? CustomerCode, string CustomerName, string Email, string Phone, string? CompanyName,
    string? Address, string? City, string? State, CustomerStatus Status, string? AssignedTo, DateTime CreatedDate)
{
    public static CustomerDto From(Customer c) => new(c.Id, c.CustomerCode, c.CustomerName, c.Email, c.Phone, c.CompanyName,
        c.Address, c.City, c.State, c.Status, c.AssignedUser?.UserName, c.CreatedDate);
}

public class CustomerInputDto
{
    [Required, StringLength(100)]
    public string CustomerName { get; set; } = "";

    [Required, StringLength(150), RegularExpression(ValidationRules.Email, ErrorMessage = ValidationRules.EmailMessage)]
    public string Email { get; set; } = "";

    [Required, RegularExpression(ValidationRules.Phone, ErrorMessage = ValidationRules.PhoneMessage)]
    public string Phone { get; set; } = "";

    [StringLength(100)] public string? CompanyName { get; set; }
    [StringLength(250)] public string? Address { get; set; }
    [StringLength(60)] public string? City { get; set; }
    [StringLength(60)] public string? State { get; set; }
    [EnumDataType(typeof(CustomerStatus))] public CustomerStatus Status { get; set; } = CustomerStatus.Active;
    [StringLength(500)] public string? Notes { get; set; }

    /// <summary>User id of the Sales Executive. Ignored for Sales Executives, who always own their records.</summary>
    public string? AssignedTo { get; set; }

    public void ApplyTo(Customer c)
    {
        c.CustomerName = CustomerName;
        c.Email = Email.Trim().ToLowerInvariant();
        c.Phone = Phone.Trim();
        c.CompanyName = CompanyName;
        c.Address = Address;
        c.City = City;
        c.State = State;
        c.Status = Status;
        c.Notes = Notes;
    }
}
