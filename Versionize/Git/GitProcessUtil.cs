using LibGit2Sharp;
using System.Diagnostics;
using Versionize.CommandLine;

namespace Versionize.Git;

/// <summary>
/// Used for git operations that aren't supported by LibGit2Sharp.
/// </summary>
public static class GitProcessUtil
{
    public static void CreateSignedCommit(string workingDirectory, string commitMessage)
    {
        RunCommand("git", $"-C \"{workingDirectory}\" commit -S -m \"{commitMessage}\"");
    }

    public static void CreateSignedTag(string workingDirectory, string tagName, string message)
    {
        RunCommand("git", $"-C \"{workingDirectory}\" tag -s \"{tagName}\" -m \"{message}\"");
    }

    public static bool IsCommitSigned(string workingDirectory, Commit commit)
    {
        var exitCode = RunCommand(
            "git",
            $"-C \"{workingDirectory}\" verify-commit {commit.Sha}",
            throwIfFail: false);
        return exitCode == 0;
    }

    public static bool IsTagSigned(string workingDirectory, Tag tag)
    {
        var exitCode = RunCommand(
            "git",
            $"-C \"{workingDirectory}\" verify-tag {tag.FriendlyName}",
            throwIfFail: false);
        return exitCode == 0;
    }

    public static void RunGpgCommand(string args)
    {
        RunCommand("gpg", args);
    }

    private static int RunCommand(string command, string args, bool throwIfFail = true)
    {
        var processInfo = new ProcessStartInfo(command, args)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = new Process();
        process.StartInfo = processInfo;
        process.Start();

        string output = process.StandardOutput.ReadToEnd();
        string error = process.StandardError.ReadToEnd();

        process.WaitForExit();

        if (!string.IsNullOrEmpty(output))
        {
            Debug.WriteLine(output);
        }

        if (process.ExitCode != 0 && throwIfFail)
        {
            throw new VersionizeException(ErrorMessages.CommandFailed(command, args, error, process.ExitCode), 1);
        }

        return process.ExitCode;
    }
}
