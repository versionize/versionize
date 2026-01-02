using System.Text.RegularExpressions;
using NuGet.Versioning;

namespace Versionize.BumpFiles;

/// <summary>
/// Handles version bumping in AssemblyInfo.cs files located in the Properties directory.
/// </summary>
/// <remarks>
/// <para>
/// This class discovers and updates assembly version attributes in Properties/AssemblyInfo.cs files.
/// It supports updating AssemblyVersion, AssemblyFileVersion, and other custom assembly attributes.
/// </para>
/// <para>
/// <strong>Version Format:</strong> Converts semantic versions (e.g., 2.3.4) to 4-part assembly version format (e.g., 2.3.4.0).
/// The fourth component is always set to 0. Pre-release labels are intentionally discarded as AssemblyVersion and 
/// AssemblyFileVersion do not support them (System.Version only accepts numeric components).
/// </para>
/// <para>
/// <strong>Version Element Behavior:</strong>
/// <list type="bullet">
/// <item><description><c>"Version"</c> (default) - Updates both AssemblyVersion and AssemblyFileVersion attributes</description></item>
/// <item><description><c>"AssemblyVersion"</c> - Updates only the AssemblyVersion attribute</description></item>
/// <item><description><c>"AssemblyFileVersion"</c> - Updates only the AssemblyFileVersion attribute</description></item>
/// <item><description>Custom attribute names - Updates the specified assembly attribute (e.g., AssemblyInformationalVersion)</description></item>
/// </list>
/// </para>
/// <para>
/// <strong>File Discovery:</strong> Only files at Properties/AssemblyInfo.cs relative to the project directory are discovered.
/// Files without the required version attributes are automatically ignored.
/// </para>
/// </remarks>
/// <example>
/// Example AssemblyInfo.cs content before update:
/// <code>
/// [assembly: AssemblyVersion("1.0.0.0")]
/// [assembly: AssemblyFileVersion("1.0.0.0")]
/// </code>
/// After updating to version 2.3.4:
/// <code>
/// [assembly: AssemblyVersion("2.3.4.0")]
/// [assembly: AssemblyFileVersion("2.3.4.0")]
/// </code>
/// Note: Pre-release versions like 2.3.4-alpha.1 will produce 2.3.4.0 (pre-release label is discarded).
/// </example>
public sealed class AssemblyInfoBumpFile
{
    private static readonly TimeSpan RegexTimeout = TimeSpan.FromSeconds(1);
    private static readonly Regex VersionAttributePattern = new(
        @"\[assembly:\s*(AssemblyVersion|AssemblyFileVersion)\s*\(",
        RegexOptions.Compiled,
        RegexTimeout);

    private readonly string _filePath;
    private readonly string _versionElement;

    private AssemblyInfoBumpFile(string filePath, string versionElement)
    {
        _filePath = filePath;
        _versionElement = versionElement;
    }

    /// <summary>
    /// Attempts to create an AssemblyInfoBumpFile instance for the specified project directory.
    /// </summary>
    /// <param name="projectDirectory">The project directory to search for Properties/AssemblyInfo.cs</param>
    /// <param name="versionElement">The version element/attribute to update. Defaults to "Version" if not specified.</param>
    /// <returns>
    /// An AssemblyInfoBumpFile instance if Properties/AssemblyInfo.cs exists and contains the required version attributes;
    /// otherwise, null.
    /// </returns>
    public static AssemblyInfoBumpFile? TryCreate(string projectDirectory, string versionElement)
    {
        var assemblyInfoPath = Path.Combine(projectDirectory, "Properties", "AssemblyInfo.cs");
        
        if (!File.Exists(assemblyInfoPath))
        {
            return null;
        }

        if (!IsVersionable(assemblyInfoPath, versionElement))
        {
            return null;
        }

        return new AssemblyInfoBumpFile(assemblyInfoPath, versionElement);
    }

    /// <summary>
    /// Updates the version attributes in the AssemblyInfo.cs file.
    /// </summary>
    /// <param name="version">The semantic version to write. Will be converted to 4-part format (Major.Minor.Patch.0).</param>
    /// <remarks>
    /// <para>When versionElement is "Version", both AssemblyVersion and AssemblyFileVersion are updated.</para>
    /// <para>For other values, only the specified attribute is updated.</para>
    /// <para>Pre-release labels are intentionally discarded as AssemblyVersion and AssemblyFileVersion only support numeric components.</para>
    /// </remarks>
    public void WriteVersion(SemanticVersion version)
    {
        var content = File.ReadAllText(_filePath);
        var versionString = $"{version.Major}.{version.Minor}.{version.Patch}.0";

        if (_versionElement == "Version")
        {
            content = UpdateAttribute(content, "AssemblyVersion", versionString);
            content = UpdateAttribute(content, "AssemblyFileVersion", versionString);
        }
        else
        {
            content = UpdateAttribute(content, _versionElement, versionString);
        }

        File.WriteAllText(_filePath, content);
    }

    /// <summary>
    /// Gets the full path to the AssemblyInfo.cs file.
    /// </summary>
    public string FilePath => _filePath;

    private static bool IsVersionable(string filePath, string versionElement)
    {
        var content = File.ReadAllText(filePath);

        if (versionElement == "Version")
        {
            return VersionAttributePattern.IsMatch(content);
        }

        var pattern = new Regex(
            @$"\[assembly:\s*{Regex.Escape(versionElement)}\s*\(",
            RegexOptions.Compiled,
            RegexTimeout);
        return pattern.IsMatch(content);
    }

    private static string UpdateAttribute(string content, string attributeName, string version)
    {
        var pattern = new Regex(
            @$"\[assembly:\s*{Regex.Escape(attributeName)}\s*\(\s*""[^""]*""\s*\)\s*\]",
            RegexOptions.Compiled,
            RegexTimeout);
        var replacement = $"[assembly: {attributeName}(\"{version}\")]";
        return pattern.Replace(content, replacement);
    }
}
