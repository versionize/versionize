﻿using System.Text.RegularExpressions;
using Versionize.ConventionalCommits;
using Version = NuGet.Versioning.SemanticVersion;

namespace Versionize.Changelog;

public sealed partial class AzureLinkBuilder : IChangelogLinkBuilder
{
    private readonly string _organization;
    private readonly string _repository;

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

    public string BuildVersionTagLink(Version version)
    {
        return $"https://{_organization}@dev.azure.com/{_organization}/{_repository}?version=GTv{version}";
    }

    public string BuildIssueLink(string issueId)
    {
        return $"https://{_organization}@dev.azure.com/{_organization}/{_repository}/_workitems/edit/{issueId}";
    }

    public string BuildCommitLink(ConventionalCommit commit)
    {
        return $"https://{_organization}@dev.azure.com/{_organization}/{_repository}/commit/{commit.Sha}";
    }

    [GeneratedRegex("^git@ssh.dev.azure.com:(?<organization>.*?)/(?<repository>.*?)(?:\\.git)?$")]
    private static partial Regex SshRegex();

    [GeneratedRegex("^https://(?<organization>.*?)@dev.azure.com/(?<organization>.*?)/(?<repository>.*?)(?:\\.git)?$")]
    private static partial Regex HttpsRegex();
}
