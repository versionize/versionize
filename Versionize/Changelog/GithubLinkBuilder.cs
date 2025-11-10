using System.Text.RegularExpressions;
using Versionize.ConventionalCommits;

namespace Versionize.Changelog;

public sealed partial class GithubLinkBuilder : IChangelogLinkBuilder
{
    private readonly string _organization;
    private readonly string _repository;

    public GithubLinkBuilder(string pushUrl)
    {
        if (pushUrl.StartsWith("git@github.com:"))
        {
            var regex = SshRegex();
            var matches = regex.Match(pushUrl);

            if (!matches.Success)
            {
                throw new InvalidOperationException($"Remote url {pushUrl} is not recognized as valid GitHub SSH pattern");
            }

            _organization = matches.Groups["organization"].Value;
            _repository = matches.Groups["repository"].Value;
        }
        else if (pushUrl.StartsWith("https://github.com/"))
        {
            var regex = HttpsRegex();
            var matches = regex.Match(pushUrl);

            if (!matches.Success)
            {
                throw new InvalidOperationException($"Remote url {pushUrl} is not recognized as valid GitHub HTTPS pattern");
            }
            _organization = matches.Groups["organization"].Value;
            _repository = matches.Groups["repository"].Value;
        }
        else
        {
            throw new InvalidOperationException($"Remote url {pushUrl} is not recognized as GitHub SSH or HTTPS url");
        }
    }

    public static bool IsPushUrl(string pushUrl)
    {
        return pushUrl.StartsWith("git@github.com:") || pushUrl.StartsWith("https://github.com/");
    }

    public string BuildVersionTagLink(string currentTag, string previousTag)
    {
        return $"https://www.github.com/{_organization}/{_repository}/releases/tag/{currentTag}";
    }

    public string BuildIssueLink(string issueId)
    {
        return $"https://www.github.com/{_organization}/{_repository}/issues/{issueId}";
    }

    public string BuildCommitLink(ConventionalCommit commit)
    {
        return $"https://www.github.com/{_organization}/{_repository}/commit/{commit.Sha}";
    }

    [GeneratedRegex("^git@github.com:(?<organization>.*?)/(?<repository>.*?)(?:\\.git)?$")]
    private static partial Regex SshRegex();

    [GeneratedRegex("^https://github.com/(?<organization>.*?)/(?<repository>.*?)(?:\\.git)?$")]
    private static partial Regex HttpsRegex();
}
