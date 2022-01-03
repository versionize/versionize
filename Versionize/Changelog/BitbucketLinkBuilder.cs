using System;
using System.Text.RegularExpressions;
using Version = NuGet.Versioning.SemanticVersion;

namespace Versionize.Changelog
{
    public class BitbucketLinkBuilder : IChangelogLinkBuilder
    {
        private readonly string _organization;
        private readonly string _repository;

        public BitbucketLinkBuilder(string pushUrl)
        {
            if (pushUrl.StartsWith("git@bitbucket.org:"))
            {
                var httpsPattern = new Regex("^git@bitbucket.org:(?<organization>.*?)/(?<repository>.*?)(?:\\.git)?$");
                var matches = httpsPattern.Match(pushUrl);

                if (!matches.Success)
                {
                    throw new InvalidOperationException($"Remote url {pushUrl} is not recognized as valid Bitbucket SSH pattern");
                }

                _organization = matches.Groups["organization"].Value;
                _repository = matches.Groups["repository"].Value;
            }
            else if (IsHttpsPushUrl(pushUrl))
            {
                var httpsPattern = new Regex("^https://.*?bitbucket.org/(?<organization>.*?)/(?<repository>.*?)(?:\\.git)?$");
                var matches = httpsPattern.Match(pushUrl);

                if (!matches.Success)
                {
                    throw new InvalidOperationException($"Remote url {pushUrl} is not recognized as valid Bitbucket HTTPS pattern");
                }
                _organization = matches.Groups["organization"].Value;
                _repository = matches.Groups["repository"].Value;
            }
            else
            {
                throw new InvalidOperationException($"Remote url {pushUrl} is not recognized as Bitbucket SSH or HTTPS url");
            }
        }

        public static bool IsPushUrl(string pushUrl)
        {
            return pushUrl.StartsWith("git@bitbucket.org:") || IsHttpsPushUrl(pushUrl);
        }

        public string BuildVersionTagLink(Version version)
        {
            return $"https://bitbucket.org/{_organization}/{_repository}/src/v{version}";
        }

        public string BuildCommitLink(ConventionalCommit commit)
        {
            return $"https://bitbucket.org/{_organization}/{_repository}/commits/{commit.Sha}";
        }

        private static bool IsHttpsPushUrl(string pushUrl)
        {
            return new Regex("^https://.*?@bitbucket.org/.*$").IsMatch(pushUrl);
        }

    }
}
