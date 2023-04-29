using HandlebarsDotNet;

using Version = NuGet.Versioning.SemanticVersion;

namespace Versionize.Changelog;

public class ChangelogLinkUtil
{
    public static string CreateCompareUrl(
        string compareUrlFormat,
        string owner,
        string repository,
        Version newVersion,
        Version previousVersion)
    {
        var template = Handlebars.Compile(compareUrlFormat);
        var data = new
        {
            owner,
            repository,
            currentTag = $"v{newVersion}",
            previousTag = $"v{previousVersion}",
        };

        return template(data);
    }
}
