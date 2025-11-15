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
        // Arrange
        UnityBumpFile bumpFile = TempProject.CreateUnityBumpFile(_tempDir, "0.1.2");
        var filePath = bumpFile.GetFilePaths().Single();
        var originalFileContents = File.ReadAllText(filePath);

        // Act
        bumpFile.WriteVersion(new Version(2, 3, 4));

        // Assert
        var versionedFileContents = File.ReadAllText(filePath);
        var expectedFileContents = originalFileContents.Replace("  bundleVersion: 0.1.2", "  bundleVersion: 2.3.4");
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
