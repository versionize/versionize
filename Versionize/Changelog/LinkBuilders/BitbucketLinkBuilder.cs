using System.Text.RegularExpressions;
using Versionize.ConventionalCommits;
using Versionize.CommandLine;

namespace Versionize.Changelog.LinkBuilders;

public sealed partial class BitbucketLinkBuilder : IChangelogLinkBuilder
{
    private const string OrgSshPrefix = "git@bitbucket.org:";
    private const string ComSshPrefix = "git@bitbucket.com:";

    private readonly string _organization;
    private readonly string _repository;
    private readonly string _domain;

    public BitbucketLinkBuilder(string pushUrl)
    {
        if (pushUrl.StartsWith(OrgSshPrefix) || pushUrl.StartsWith(ComSshPrefix))
        {
            var regex = SshRegex();
            var matches = regex.Match(pushUrl);

            if (!matches.Success)
            {
                throw new VersionizeException(ErrorMessages.RemoteUrlInvalidSshPattern("Bitbucket", pushUrl), 1);
            }

            _organization = matches.Groups["organization"].Value;
            _repository = matches.Groups["repository"].Value;
            _domain = matches.Groups["domain"].Value;
        }
        else if (IsHttpsPushUrl(pushUrl))
        {
            var regex = HttpsRegex();
            var matches = regex.Match(pushUrl);

            if (!matches.Success)
            {
                throw new VersionizeException(ErrorMessages.RemoteUrlInvalidHttpsPattern("Bitbucket", pushUrl), 1);
            }

            _organization = matches.Groups["organization"].Value;
            _repository = matches.Groups["repository"].Value;
            _domain = matches.Groups["domain"].Value;
        }
        else
        {
            throw new VersionizeException(ErrorMessages.RemoteUrlNotRecognized("Bitbucket", pushUrl), 1);
        }
    }

    public static bool IsPushUrl(string pushUrl)
    {
        return pushUrl.StartsWith(ComSshPrefix) || pushUrl.StartsWith(OrgSshPrefix) || IsHttpsPushUrl(pushUrl);
    }

    public string BuildVersionTagLink(string currentTag, string previousTag)
    {
        return $"https://bitbucket.{_domain}/{_organization}/{_repository}/src/{currentTag}";
    }

    public string BuildIssueLink(string issueId)
    {
        return $"https://bitbucket.{_domain}/{_organization}/{_repository}/issues/{issueId}";
    }

    public string BuildCommitLink(ConventionalCommit commit)
    {
        return $"https://bitbucket.{_domain}/{_organization}/{_repository}/commits/{commit.Sha}";
    }

    private static bool IsHttpsPushUrl(string pushUrl)
    {
        return HttpsPushUrlRegex().IsMatch(pushUrl);
    }

    [GeneratedRegex("^git@bitbucket\\.(?<domain>org|com):(?<organization>.*?)/(?<repository>.*?)(?:\\.git)?$")]
    private static partial Regex SshRegex();

    [GeneratedRegex("^https://.*?bitbucket\\.(?<domain>org|com)/(?<organization>.*?)/(?<repository>.*?)(?:\\.git)?$")]
    private static partial Regex HttpsRegex();

    [GeneratedRegex("^https://.*?@bitbucket\\.(org|com)/.*$")]
    private static partial Regex HttpsPushUrlRegex();
}
