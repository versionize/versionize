using System.Diagnostics.CodeAnalysis;
using static System.MemoryExtensions;

namespace Versionize.Versioning;

/// <summary>
/// Represents a generic version number as a sequence of non-negative integer components.
/// Unlike <see cref="System.Version"/>, this class does not assign semantic meaning (major, minor, build, revision)
/// to specific positions, allowing it to work with arbitrary version schemes (e.g., "1.2.3.4.5").
/// </summary>
public sealed class Version : IEquatable<Version>, IComparable<Version>, IComparable, ICloneable
{
    private readonly int[] _components;

    /// <summary>
    /// Initializes a new instance of the <see cref="Version"/> class with no components.
    /// </summary>
    public Version()
    {
        _components = [];
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Version"/> class from a string representation.
    /// </summary>
    /// <param name="version">A string containing version components separated by periods ('.').</param>
    public Version(string version)
    {
        if (string.IsNullOrWhiteSpace(version))
        {
            throw new ArgumentException("Version string cannot be null or whitespace.", nameof(version));
        }

        var parts = version.Split('.');
        _components = new int[parts.Length];

        for (int i = 0; i < parts.Length; i++)
        {
            if (!int.TryParse(parts[i], out int component) || component < 0)
            {
                throw new FormatException($"Version component '{parts[i]}' is not a valid non-negative integer.");
            }
            _components[i] = component;
        }
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Version"/> class from an array of components.
    /// </summary>
    /// <param name="components">An array of non-negative integers representing version components.</param>
    public Version(params int[] components)
    {
        if (components == null)
        {
            throw new ArgumentNullException(nameof(components));
        }

        if (components.Any(c => c < 0))
        {
            throw new ArgumentOutOfRangeException(nameof(components), "All version components must be non-negative.");
        }

        _components = [.. components];
    }

    /// <summary>
    /// Gets the number of components in this version.
    /// </summary>
    public int ComponentCount => _components.Length;

    /// <summary>
    /// Gets the component at the specified index.
    /// </summary>
    /// <param name="index">The zero-based index of the component.</param>
    /// <returns>The component value at the specified index, or -1 if the index is out of range.</returns>
    public int this[int index] => index >= 0 && index < _components.Length ? _components[index] : -1;

    /// <summary>
    /// Gets all components as a read-only collection.
    /// </summary>
    public IReadOnlyList<int> Components => _components;

    /// <summary>
    /// Parses a string into a <see cref="Version"/> object.
    /// </summary>
    public static Version Parse(string input)
    {
        return new Version(input);
    }

    /// <summary>
    /// Parses a read-only span of characters into a <see cref="Version"/> object.
    /// </summary>
    public static Version Parse(ReadOnlySpan<char> input)
    {
        if (input.IsEmpty || input.IsWhiteSpace())
        {
            throw new ArgumentException("Version string cannot be null or whitespace.");
        }

        var components = new List<int>();
        var remaining = input;

        while (!remaining.IsEmpty)
        {
            var separatorIndex = remaining.IndexOf('.');
            var part = separatorIndex >= 0 ? remaining[..separatorIndex] : remaining;

            if (!int.TryParse(part, out int component) || component < 0)
            {
                throw new FormatException($"Version component '{part}' is not a valid non-negative integer.");
            }
            components.Add(component);

            if (separatorIndex < 0)
            {
                break;
            }
            remaining = remaining[(separatorIndex + 1)..];
        }

        return new Version([.. components]);
    }

    /// <summary>
    /// Tries to parse a string into a <see cref="Version"/> object.
    /// </summary>
    public static bool TryParse([NotNullWhen(true)] string? input, [NotNullWhen(true)] out Version? result)
    {
        result = null;
        if (string.IsNullOrWhiteSpace(input))
        {
            return false;
        }

        try
        {
            result = new Version(input);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Tries to parse a read-only span of characters into a <see cref="Version"/> object.
    /// </summary>
    public static bool TryParse(ReadOnlySpan<char> input, [NotNullWhen(true)] out Version? result)
    {
        result = null;
        if (input.IsEmpty || input.IsWhiteSpace())
        {
            return false;
        }

        try
        {
            result = Parse(input);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Creates a new version by incrementing the component at the specified index.
    /// All components after the incremented position are reset to zero.
    /// </summary>
    /// <param name="index">The zero-based index of the component to increment.</param>
    /// <returns>A new <see cref="Version"/> with the incremented component.</returns>
    public Version Increment(int index)
    {
        if (index < 0 || index >= _components.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(index), "Index must be within the range of existing components.");
        }

        var newComponents = new int[_components.Length];
        for (int i = 0; i <= index; i++)
        {
            newComponents[i] = i == index ? _components[i] + 1 : _components[i];
        }
        // Components after the incremented index are already zero (default)

        return new Version(newComponents);
    }

    public object Clone()
    {
        return new Version(_components);
    }

    public bool Equals([NotNullWhen(true)] Version? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return _components.SequenceEqual(other._components);
    }

    public override bool Equals([NotNullWhen(true)] object? obj)
    {
        return Equals(obj as Version);
    }

    public override int GetHashCode()
    {
        var hash = new HashCode();
        foreach (var component in _components)
        {
            hash.Add(component);
        }
        return hash.ToHashCode();
    }

    public int CompareTo(Version? other)
    {
        if (other is null)
        {
            return 1;
        }

        int minLength = Math.Min(_components.Length, other._components.Length);

        for (int i = 0; i < minLength; i++)
        {
            int comparison = _components[i].CompareTo(other._components[i]);
            if (comparison != 0)
            {
                return comparison;
            }
        }

        // If all compared components are equal, the longer version is considered greater
        return _components.Length.CompareTo(other._components.Length);
    }

    public int CompareTo(object? obj)
    {
        if (obj is null)
        {
            return 1;
        }

        if (obj is not Version version)
        {
            throw new ArgumentException("Object must be of type Version.", nameof(obj));
        }

        return CompareTo(version);
    }

    public override string ToString()
    {
        return string.Join('.', _components);
    }

    public string ToString(int componentCount)
    {
        if (componentCount < 0 || componentCount > _components.Length)
        {
            throw new ArgumentException("Component count must be between 0 and the number of components.", nameof(componentCount));
        }

        if (componentCount == 0)
        {
            return string.Empty;
        }

        return string.Join('.', _components.Take(componentCount));
    }

    public static bool operator ==(Version? v1, Version? v2)
    {
        if (v1 is null)
        {
            return v2 is null;
        }
        return v1.Equals(v2);
    }

    public static bool operator !=(Version? v1, Version? v2)
    {
        return !(v1 == v2);
    }

    public static bool operator <(Version? v1, Version? v2)
    {
        if (v1 is null)
        {
            return v2 is not null;
        }
        return v1.CompareTo(v2) < 0;
    }

    public static bool operator >(Version? v1, Version? v2)
    {
        if (v1 is null)
        {
            return false;
        }
        return v1.CompareTo(v2) > 0;
    }

    public static bool operator <=(Version? v1, Version? v2)
    {
        if (v1 is null)
        {
            return true;
        }
        return v1.CompareTo(v2) <= 0;
    }

    public static bool operator >=(Version? v1, Version? v2)
    {
        if (v1 is null)
        {
            return v2 is null;
        }
        return v1.CompareTo(v2) >= 0;
    }
}
