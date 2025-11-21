using System.ComponentModel.DataAnnotations;
using McMaster.Extensions.CommandLineUtils;
using McMaster.Extensions.CommandLineUtils.Validation;
using NuGet.Versioning;

namespace Versionize.Config.Validation;

internal sealed class SemanticVersionValidator : IOptionValidator
{
    public static readonly SemanticVersionValidator Default = new();

    public ValidationResult? GetValidationResult(CommandOption option, ValidationContext context)
    {
        var value = option.Value();
        if (string.IsNullOrEmpty(value))
        {
            return ValidationResult.Success;
        }

        return SemanticVersion.TryParse(value, out _)
            ? ValidationResult.Success
            : new ValidationResult($"The value '{option.Value()}' is not a valid semantic version.");
    }
}

internal sealed class SemanticVersionAttribute : ValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext context)
    {
        var strValue = value as string;
        if (string.IsNullOrEmpty(strValue))
        {
            return ValidationResult.Success;
        }

        return SemanticVersion.TryParse(strValue, out _)
            ? ValidationResult.Success
            : new ValidationResult($"The value '{strValue}' is not a valid semantic version.");
    }
}
