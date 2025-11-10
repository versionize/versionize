using System.Text.RegularExpressions;
using Versionize.ConventionalCommits;

namespace Versionize.Changelog;

public sealed partial class AzureLinkBuilder : IChangelogLinkBuilder
{
    private readonly string _organization;
    private readonly string _repository;
    private readonly string _project;

    public AzureLinkBuilder(string pushUrl)
    {
        if (pushUrl.StartsWith("git@ssh.dev.azure.com:"))
        {
            var regex = SshRegex();
            var matches = regex.Match(pushUrl);

            if (!matches.Success)
            {
                throw new InvalidOperationException($"Remote url {pushUrl} is not recognized as valid Azure SSH pattern");
            }

            _organization = matches.Groups["organization"].Value;
            _repository = matches.Groups["repository"].Value;
            _project = matches.Groups["project"].Value;
        }
        else if (pushUrl.StartsWith("https://") && pushUrl.Contains("@dev.azure.com/"))
        {
            var regex = HttpsRegex();
            var matches = regex.Match(pushUrl);

            if (!matches.Success)
            {
                throw new InvalidOperationException($"Remote url {pushUrl} is not recognized as valid Azure HTTPS pattern");
            }
            _organization = matches.Groups["organization"].Value;
            _repository = matches.Groups["repository"].Value;
            _project = matches.Groups["project"].Value;
        }
        else
        {
            throw new InvalidOperationException($"Remote url {pushUrl} is not recognized as Azure SSH or HTTPS url");
        }
    }

    public static bool IsPushUrl(string pushUrl)
    {
        return pushUrl.StartsWith("git@ssh.dev.azure.com:") || (pushUrl.StartsWith("https://") && pushUrl.Contains("@dev.azure.com/"));
    }

    public string BuildVersionTagLink(string currentTag, string previousTag)
    {
        return $"https://dev.azure.com/{_organization}/{_project}/_git/{_repository}?version=GT{currentTag}";
    }

    public string BuildIssueLink(string issueId)
    {
        return $"https://dev.azure.com/{_organization}/{_project}/_workitems/edit/{issueId}";
    }

    public string BuildCommitLink(ConventionalCommit commit)
    {
        return $"https://dev.azure.com/{_organization}/{_project}/_git/{_repository}/commit/{commit.Sha}";
    }

    [GeneratedRegex("^git@ssh.dev.azure.com:(?<version>.*?)/(?<organization>.*?)/(?<project>.*?)/(?<repository>.*?)(?:\\.git)?$")]
    private static partial Regex SshRegex();

    [GeneratedRegex("^https://(?<organization>.*?)@dev.azure.com/(?<organization>.*?)/(?<project>.*?)/_git/(?<repository>.*?)(?:\\.git)?$")]
    private static partial Regex HttpsRegex();
}
