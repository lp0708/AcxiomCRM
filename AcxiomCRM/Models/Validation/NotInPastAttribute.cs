using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace AcxiomCRM.Models.Validation;

/// <summary>
/// Rejects dates earlier than today, unless the named status property holds one of the exempt values
/// (e.g. a Won/Lost opportunity or a Completed follow-up may keep a past date).
/// Validated on the server and, via wwwroot/js/validation.js, in the browser.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class NotInPastAttribute : ValidationAttribute, IClientModelValidator
{
    private readonly string _statusProperty;
    private readonly string[] _exemptStatuses;

    public NotInPastAttribute(string statusProperty, params string[] exemptStatuses)
    {
        _statusProperty = statusProperty;
        _exemptStatuses = exemptStatuses;
    }

    protected override ValidationResult? IsValid(object? value, ValidationContext context)
    {
        if (value is not DateTime date) return ValidationResult.Success;

        var status = context.ObjectType.GetProperty(_statusProperty)?.GetValue(context.ObjectInstance)?.ToString();
        if (status != null && _exemptStatuses.Contains(status)) return ValidationResult.Success;

        return date.Date < DateTime.Today
            ? new ValidationResult(ErrorMessage ?? $"{context.DisplayName} cannot be in the past.", new[] { context.MemberName! })
            : ValidationResult.Success;
    }

    public void AddValidation(ClientModelValidationContext context)
    {
        context.Attributes["data-val"] = "true";
        context.Attributes["data-val-notinpast"] = ErrorMessage ?? $"{context.ModelMetadata.GetDisplayName()} cannot be in the past.";
        context.Attributes["data-val-notinpast-statusfield"] = _statusProperty;
        context.Attributes["data-val-notinpast-exempt"] = string.Join(",", _exemptStatuses);
    }
}
