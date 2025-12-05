using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Versionize.Utils;

public static class ThrowHelper
{
    public static T ThrowIfNull<T>(
        [NotNull] T? argument,
        [CallerArgumentExpression(nameof(argument))] string? paramName = null)
    {
        return argument ?? throw new ArgumentNullException(paramName);
    }
}
