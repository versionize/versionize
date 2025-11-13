using System.ComponentModel.DataAnnotations;
using McMaster.Extensions.CommandLineUtils;
using McMaster.Extensions.CommandLineUtils.Validation;

namespace Versionize.Config.Validation;

internal class PrereleaseIdentifierValidator : IOptionValidator
{
    public static readonly PrereleaseIdentifierValidator Default = new();

    public ValidationResult? GetValidationResult(CommandOption option, ValidationContext context)
    {
        var value = option.Value();
        if (string.IsNullOrEmpty(value))
        {
            return ValidationResult.Success;
        }

        return IsValid(value)
            ? ValidationResult.Success
            : new ValidationResult($"The value '{option.Value()}' is not a valid semantic version prerelease identifier.");
    }

    internal static bool IsValid(string s)
    {
        // Numeric identifiers must not include leading zeroes
        if (s.Length > 1 && s[0] == '0')
        {
            bool flag = true;
            for (int i = 1; i < s.Length; i++)
            {
                if (!char.IsDigit(s[i]))
                {
                    flag = false;
                    break;
                }
            }

            if (flag)
            {
                return false;
            }
        }

        for (int j = 0; j < s.Length; j++)
        {
            if (!char.IsAsciiLetterOrDigit(s[j]) && s[j] != '-')
            {
                return false;
            }
        }

        return true;
    }
}
