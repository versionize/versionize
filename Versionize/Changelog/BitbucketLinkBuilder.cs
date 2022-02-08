using System.Text.RegularExpressions;
using Version = NuGet.Versioning.SemanticVersion;

namespace Versionize.Changelog;

// TODO: Accept both .org and .com extensions
public class BitbucketLinkBuilder : IChangelogLinkBuilder
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
            var httpsPattern = new Regex("^git@bitbucket\\.(?<domain>org|com):(?<organization>.*?)/(?<repository>.*?)(?:\\.git)?$");
            var matches = httpsPattern.Match(pushUrl);

            if (!matches.Success)
            {
                throw new InvalidOperationException($"Remote url {pushUrl} is not recognized as valid Bitbucket SSH pattern");
            }

            _organization = matches.Groups["organization"].Value;
            _repository = matches.Groups["repository"].Value;
            _domain = matches.Groups["domain"].Value;
        }
        else if (IsHttpsPushUrl(pushUrl))
        {
            var httpsPattern = new Regex("^https://.*?bitbucket\\.(?<domain>org|com)/(?<organization>.*?)/(?<repository>.*?)(?:\\.git)?$");
            var matches = httpsPattern.Match(pushUrl);

            if (!matches.Success)
            {
                throw new InvalidOperationException($"Remote url {pushUrl} is not recognized as valid Bitbucket HTTPS pattern");
            }

            _organization = matches.Groups["organization"].Value;
            _repository = matches.Groups["repository"].Value;
            _domain = matches.Groups["domain"].Value;
        }
        else
        {
            throw new InvalidOperationException($"Remote url {pushUrl} is not recognized as Bitbucket SSH or HTTPS url");
        }
    }

    public static bool IsPushUrl(string pushUrl)
    {
        return pushUrl.StartsWith(ComSshPrefix) || pushUrl.StartsWith(OrgSshPrefix) || IsHttpsPushUrl(pushUrl);
    }

    public string BuildVersionTagLink(Version version)
    {
        return $"https://bitbucket.{_domain}/{_organization}/{_repository}/src/v{version}";
    }

    public string BuildCommitLink(ConventionalCommit commit)
    {
        return $"https://bitbucket.{_domain}/{_organization}/{_repository}/commits/{commit.Sha}";
    }

    private static bool IsHttpsPushUrl(string pushUrl)
    {
        return new Regex("^https://.*?@bitbucket\\.(org|com)/.*$").IsMatch(pushUrl);
    }
}
