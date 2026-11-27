using System.Globalization;

namespace ActivityTracker.Core.Services;

/// <summary>
/// A semantic version (<c>1.2.3</c>, <c>1.1.1-dev.42</c>) ordered by the SemVer 2.0 rules:
/// a pre-release is older than its release, and pre-release parts compare numerically when both are numbers.
/// </summary>
public sealed class SemVersion : IComparable<SemVersion>, IEquatable<SemVersion>
{
    private SemVersion(int major, int minor, int patch, string[] prerelease)
    {
        Major = major;
        Minor = minor;
        Patch = patch;
        Prerelease = prerelease;
    }

    public int Major { get; }

    public int Minor { get; }

    public int Patch { get; }

    /// <summary>Dot-separated pre-release parts, e.g. ["dev", "42"]; empty for a release.</summary>
    public IReadOnlyList<string> Prerelease { get; }

    public bool IsPrerelease => Prerelease.Count > 0;

    /// <summary>Parses "1.2.3", "v1.2.3" or "1.2.3-dev.4+build"; build metadata after '+' is ignored.</summary>
    public static bool TryParse(string? text, out SemVersion version)
    {
        version = null!;
        var s = text?.Trim() ?? "";
        if (s.StartsWith('v') || s.StartsWith('V'))
        {
            s = s[1..];
        }

        s = s.Split('+')[0];
        var dash = s.IndexOf('-');
        var core = dash < 0 ? s : s[..dash];
        var pre = dash < 0 ? [] : s[(dash + 1)..].Split('.');
        var parts = core.Split('.');
        if (parts.Length != 3 || pre.Any(p => p.Length == 0))
        {
            return false;
        }

        var numbers = new int[3];
        for (var i = 0; i < 3; i++)
        {
            if (!int.TryParse(parts[i], NumberStyles.None, CultureInfo.InvariantCulture, out numbers[i]))
            {
                return false;
            }
        }

        version = new SemVersion(numbers[0], numbers[1], numbers[2], pre);
        return true;
    }

    /// <exception cref="FormatException">Not a semantic version.</exception>
    public static SemVersion Parse(string text) =>
        TryParse(text, out var version) ? version : throw new FormatException($"'{text}' is not a semantic version.");

    public int CompareTo(SemVersion? other)
    {
        if (other is null)
        {
            return 1;
        }

        var byNumbers = (Major, Minor, Patch).CompareTo((other.Major, other.Minor, other.Patch));
        if (byNumbers != 0)
        {
            return byNumbers;
        }

        // A release is newer than any of its pre-releases.
        if (IsPrerelease != other.IsPrerelease)
        {
            return IsPrerelease ? -1 : 1;
        }

        for (var i = 0; i < Math.Min(Prerelease.Count, other.Prerelease.Count); i++)
        {
            var result = ComparePart(Prerelease[i], other.Prerelease[i]);
            if (result != 0)
            {
                return result;
            }
        }

        return Prerelease.Count.CompareTo(other.Prerelease.Count);
    }

    public bool Equals(SemVersion? other) => CompareTo(other) == 0;

    public override bool Equals(object? obj) => obj is SemVersion other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(Major, Minor, Patch, string.Join('.', Prerelease));

    public override string ToString() => IsPrerelease ? $"{Major}.{Minor}.{Patch}-{string.Join('.', Prerelease)}" : $"{Major}.{Minor}.{Patch}";

    public static bool operator >(SemVersion a, SemVersion b) => a.CompareTo(b) > 0;

    public static bool operator <(SemVersion a, SemVersion b) => a.CompareTo(b) < 0;

    public static bool operator >=(SemVersion a, SemVersion b) => a.CompareTo(b) >= 0;

    public static bool operator <=(SemVersion a, SemVersion b) => a.CompareTo(b) <= 0;

    /// <summary>Numbers compare as numbers and come before words; words compare as text.</summary>
    private static int ComparePart(string a, string b)
    {
        var aIsNumber = long.TryParse(a, NumberStyles.None, CultureInfo.InvariantCulture, out var aNumber);
        var bIsNumber = long.TryParse(b, NumberStyles.None, CultureInfo.InvariantCulture, out var bNumber);
        return (aIsNumber, bIsNumber) switch
        {
            (true, true) => aNumber.CompareTo(bNumber),
            (true, false) => -1,
            (false, true) => 1,
            _ => string.CompareOrdinal(a, b),
        };
    }
}
