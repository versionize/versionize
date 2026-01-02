using System.Diagnostics;

namespace Versionize.Tests.TestSupport;

/// <summary>
/// Helper utilities for GPG availability checking in tests.
/// </summary>
public sealed class GpgTestHelper
{
    /// <summary>
    /// Checks if GPG is installed and available in the system PATH.
    /// </summary>
    /// <returns>True if GPG is available; otherwise, false.</returns>
    public static bool IsGpgAvailable()
    {
        try
        {
            var processInfo = new ProcessStartInfo("gpg", "--version")
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = new Process();
            process.StartInfo = processInfo;
            if (!process.Start())
            {
                return false;
            }

            if (!process.WaitForExit(TimeSpan.FromSeconds(10)))
            {
                process.Kill();
                return false;
            }

            return process.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Verifies that GPG is available and throws an exception with installation instructions if not.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when GPG is not installed or not available in PATH.</exception>
    public static void RequireGpg()
    {
        if (!IsGpgAvailable())
        {
            throw new InvalidOperationException(
                "GPG is not installed or not available in PATH.\n" +
                "To run GPG signing tests, please install GPG:\n" +
                "  Windows: choco install gnupg or download from https://gnupg.org/download/\n" +
                "  Linux:   sudo apt-get install gnupg or sudo yum install gnupg\n" +
                "  macOS:   brew install gnupg");
        }
    }
}
