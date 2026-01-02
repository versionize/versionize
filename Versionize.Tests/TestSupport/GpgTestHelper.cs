using System.Diagnostics;

namespace Versionize.Tests.TestSupport;

public static class GpgTestHelper
{
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
            process.Start();
            process.WaitForExit();
            return process.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }

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
