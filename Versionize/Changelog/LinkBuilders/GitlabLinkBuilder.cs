using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Versionize.ConventionalCommits;
using Versionize.CommandLine;

namespace Versionize.Changelog.LinkBuilders;

public sealed partial class GitlabLinkBuilder : IChangelogLinkBuilder, IUsernameResolver
{
    private static readonly HttpClient HttpClient = new();

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
                throw new VersionizeException(ErrorMessages.RemoteUrlInvalidSshPattern("GitLab", pushUrl), 1);
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
                throw new VersionizeException(ErrorMessages.RemoteUrlInvalidHttpsPattern("GitLab", pushUrl), 1);
            }
            _organization = matches.Groups["organization"].Value;
            _repository = matches.Groups["repository"].Value;
        }
        else
        {
            throw new VersionizeException(ErrorMessages.RemoteUrlNotRecognized("GitLab", pushUrl), 1);
        }
    }

    public static bool IsPushUrl(string pushUrl)
    {
        return pushUrl.StartsWith("git@gitlab.com:") || pushUrl.StartsWith("https://gitlab.com/");
    }

    public string BuildVersionTagLink(string currentTag, string previousTag)
    {
        return $"https://gitlab.com/{_organization}/{_repository}/-/tags/{currentTag}";
    }

    public string BuildIssueLink(string issueId)
    {
        return $"https://gitlab.com/{_organization}/{_repository}/-/issues/{issueId}";
    }

    public string BuildCommitLink(ConventionalCommit commit)
    {
        return $"https://gitlab.com/{_organization}/{_repository}/-/commit/{commit.Sha}";
    }

    public string? ResolveUsername(string commitSha)
    {
        try
        {
            var fullPath = $"{_organization}/{_repository}";
            var query = $$"""
                {
                  project(fullPath: "{{fullPath}}") {
                    repository {
                      commit(id: "{{commitSha}}") {
                        author {
                          username
                        }
                      }
                    }
                  }
                }
                """;
            var requestBody = JsonSerializer.Serialize(new { query });
            var content = new StringContent(requestBody, Encoding.UTF8, "application/json");
            HttpClient.DefaultRequestHeaders.UserAgent.TryParseAdd("versionize");
            var response = HttpClient.PostAsync("https://gitlab.com/api/graphql", content).GetAwaiter().GetResult();
            var responseBody = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            using var doc = JsonDocument.Parse(responseBody);
            if (doc.RootElement.TryGetProperty("data", out var data) &&
                data.TryGetProperty("project", out var project) &&
                project.ValueKind != JsonValueKind.Null &&
                project.TryGetProperty("repository", out var repository) &&
                repository.ValueKind != JsonValueKind.Null &&
                repository.TryGetProperty("commit", out var commit) &&
                commit.ValueKind != JsonValueKind.Null &&
                commit.TryGetProperty("author", out var author) &&
                author.ValueKind != JsonValueKind.Null &&
                author.TryGetProperty("username", out var usernameElement))
            {
                return usernameElement.GetString();
            }
            return null;
        }
        catch
        {
            return null;
        }
    }

    [GeneratedRegex("^git@gitlab.com:(?<organization>.*?)/(?<repository>.*?)(?:\\.git)?$")]
    private static partial Regex SshRegex();

    [GeneratedRegex("^https://gitlab.com/(?<organization>.*?)/(?<repository>.*?)(?:\\.git)?$")]
    private static partial Regex HttpsRegex();
}
