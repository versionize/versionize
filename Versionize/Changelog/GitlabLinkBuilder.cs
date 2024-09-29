using System.Text.RegularExpressions;
using Versionize.ConventionalCommits;
using Version = NuGet.Versioning.SemanticVersion;

namespace Versionize.Changelog;

public sealed partial class GitlabLinkBuilder : IChangelogLinkBuilder
{
    private readonly string _organization;
    private readonly string _repository;

    public GitlabLinkBuilder(string pushUrl)
    {
        if (pushUrl.StartsWith("git@gitlab.com:"))
        {
            var regex = SshRegex();
            var matches = regex.Match(pushUrl);

            if (!matches.Success)
            {
                throw new InvalidOperationException($"Remote url {pushUrl} is not recognized as valid GitLab SSH pattern");
            }

            _organization = matches.Groups["organization"].Value;
            _repository = matches.Groups["repository"].Value;
        }
        else if (pushUrl.StartsWith("https://gitlab.com/"))
        {
            var regex = HttpsRegex();
            var matches = regex.Match(pushUrl);

            if (!matches.Success)
            {
                throw new InvalidOperationException($"Remote url {pushUrl} is not recognized as valid GitLab HTTPS pattern");
            }
            _organization = matches.Groups["organization"].Value;
            _repository = matches.Groups["repository"].Value;
        }
        else
        {
            throw new InvalidOperationException($"Remote url {pushUrl} is not recognized as GitLab SSH or HTTPS url");
        }
    }

    public static bool IsPushUrl(string pushUrl)
    {
        return pushUrl.StartsWith("git@gitlab.com:") || pushUrl.StartsWith("https://gitlab.com/");
    }

    public string BuildVersionTagLink(Version version)
    {
        return $"https://gitlab.com/{_organization}/{_repository}/-/tags/v{version}";
    }

    public string BuildIssueLink(string issueId)
    {
        return $"https://gitlab.com/{_organization}/{_repository}/-/issues/{issueId}";
    }

    public string BuildCommitLink(ConventionalCommit commit)
    {
        return $"https://gitlab.com/{_organization}/{_repository}/-/commit/{commit.Sha}";
    }

    [GeneratedRegex("^git@gitlab.com:(?<organization>.*?)/(?<repository>.*?)(?:\\.git)?$")]
    private static partial Regex SshRegex();

    [GeneratedRegex("^https://gitlab.com/(?<organization>.*?)/(?<repository>.*?)(?:\\.git)?$")]
    private static partial Regex HttpsRegex();
}
