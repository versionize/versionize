using Shouldly;
using Versionize.Tests.TestSupport;
using Xunit;
using Version = NuGet.Versioning.SemanticVersion;

namespace Versionize.BumpFiles;

public class UnityBumpFileTests : IDisposable
{
    private readonly string _tempDir;

    public UnityBumpFileTests()
    {
        _tempDir = TempDir.Create();
    }

    [Fact]
    public void ShouldUpdateTheVersionElementOnly()
    {
        var sourceFilePath = "TestData/ProjectSettings.asset";
        var targetFilePath = Path.Combine(_tempDir, "ProjectSettings", "ProjectSettings.asset");
        Directory.CreateDirectory(Path.Combine(_tempDir, "ProjectSettings"));
        File.Copy(sourceFilePath, targetFilePath);
        Assert.True(File.Exists(targetFilePath));
        var fileContents = File.ReadAllText(sourceFilePath);

        IBumpFile bumpFile = UnityBumpFile.Create(_tempDir);
        bumpFile.Version.ShouldBe(new Version(0, 1, 2));
        bumpFile.WriteVersion(new Version(2, 3, 4));

        var versionedFileContents = File.ReadAllText(targetFilePath);
        var expectedFileContents = fileContents.Replace("  bundleVersion: 0.1.2", "  bundleVersion: 2.3.4");
        versionedFileContents.ShouldBe(expectedFileContents);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, true);
        }
    }
}
