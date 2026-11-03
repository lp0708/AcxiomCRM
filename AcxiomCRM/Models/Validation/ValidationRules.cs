namespace AcxiomCRM.Models.Validation;

public static class ValidationRules
{
    public const string Email = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";
    public const string EmailMessage = "Enter a valid email address.";

    // Project rule: 10-digit Indian mobile number.
    public const string Phone = @"^[6-9]\d{9}$";
    public const string PhoneMessage = "Enter a valid phone number.";
}
