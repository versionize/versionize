namespace Versionize.CommandLine;

/// <summary>
/// Centralized error message templates to keep wording consistent across the application.
/// Use these helpers when calling CommandLineUI.Exit to ensure a unified style.
/// </summary>
public static class ErrorMessages
{
    // Repository errors
    public static string RepositoryNotGit(string path) =>
        $"Directory {path} or any parent directory do not contain a git working copy";
    public static string RepositoryDirty(string path, string dirtyFiles) =>
        $"Repository {path} is dirty. Please commit your changes:\n{dirtyFiles}";
    public static string GitConfigMissing() => """
        Warning: Git configuration is missing. Please configure git before running versionize:
        git config --global user.name ""John Doe""
        git config --global user.email johndoe@example.com
        """;

    // Version errors
    public static string NoVersionFound() => "No version found";
    public static string TagForVersionNotFound(string version) => $"Tag for version '{version}' not found";
    public static string InvalidVersionFormat(string input) => $"Invalid version format '{input}'";
    public static string VersionUnaffected(string currentVersion) =>
        $"Version was not affected by commits since last release ({currentVersion})";
    public static string CouldNotParseReleaseVersion(string releaseAs) =>
        $"Could not parse the specified release version {releaseAs} as valid version";
    public static string SemanticVersionConflict(string next, string current) =>
        $"Semantic versioning conflict: the next version {next} would be lower than the current version {current}. " +
        "This can be caused by using a wrong pre-release label or release as version";
    public static string VersionAlreadyExists(string version) =>
        $"Version {version} already exists. Please use a different version.";

    // Configuration errors
    public static string RepositoryDoesNotExist(string path) => $"Directory {path} does not exist";
    public static string DuplicateChangelogPaths() => "Two or more projects have changelog paths pointing to the same location.";
    public static string FailedToParseVersionizeFile(string error) => $"Failed to parse .versionize file: {error}";
    public static string CommandFailed(string command, string args, string error, int exitCode) =>
        $"Command '{command} {args}' failed with error '{error}' (exit code {exitCode})";
    public static string LibGitNotFound(Exception e) => $"""
        Error: LibGit2Sharp.NotFoundException

        This is most likely caused by running versionize against a git repository cloned with depth --1.
        In case you're using the actions/checkout@v2 in github actions you could specify fetch-depth: '1'.
        For more detail see  https://github.com/actions/checkout

        Exception detail:

        {e}
        """;

    // Remote URL errors
    public static string RemoteUrlInvalidSshPattern(string provider, string pushUrl) =>
        $"Remote url {pushUrl} is not recognized as valid {provider} SSH pattern";
    public static string RemoteUrlInvalidHttpsPattern(string provider, string pushUrl) =>
        $"Remote url {pushUrl} is not recognized as valid {provider} HTTPS pattern";
    public static string RemoteUrlNotRecognized(string provider, string pushUrl) =>
        $"Remote url {pushUrl} is not recognized as {provider} SSH or HTTPS url";

    // Dotnet bump file errors
    public static string ProjectMissingOrEmptyVersionElement(string projectFile, string versionElement) =>
        $"Project {projectFile} contains no or an empty <{versionElement}> XML Element. Please add one if you want to " +
        $"version this project - for example use <{versionElement}>1.0.0</{versionElement}>";
    public static string ProjectInvalidVersionValue(string projectFile, string versionString, string versionElement) =>
        $"Project {projectFile} contains an invalid version {versionString}. Please fix the currently contained version " +
        $"- for example use <{versionElement}>1.0.0</{versionElement}>";
    public static string ProjectMissingVersionElement(string projectFile, string versionElement) =>
        $"Project {projectFile} does not contain a <{versionElement}> XML Element. Please add one if you want to " +
        $"version this project - for example use <{versionElement}>1.0.0</{versionElement}>";
    public static string ProjectInvalidXmlFile(string projectFile) =>
        $"Project {projectFile} is not a valid xml project file. Please make sure that you have a valid project file in place!";
    public static string InconsistentProjectVersions(string workingDirectory, string versionElement) =>
        $"Some projects in '{workingDirectory}' have an inconsistent <{versionElement}> value. Align all versions " +
        $"or remove <{versionElement}> from projects that should not be versioned.";
    public static string NoVersionableProjects(string workingDirectory, string versionElement) =>
        $"No project files in '{workingDirectory}' contain a <{versionElement}> element.";
    public static string InvalidVersionElement(string element) =>
        $"Version element '{element}' is invalid. Only alphanumeric and underscore characters are allowed.";

    // Unity bump file errors
    public static string UnityProjectSettingsNotFound() => "ProjectSettings.asset not found";
    public static string UnityVersionParseFailed() => "Version could not be parsed from ProjectSettings.asset";

    // Versioning errors
    public static string PrereleaseNumberParseError(string version) =>
        $"Could not parse prerelease number from version {version}. Expected format: <label>.<number>";
    public static string BumpFileTypeNotImplemented(string bumpFileType) => $"Bump file type {bumpFileType} is not implemented";
    public static string VersionImpactCannotBeHandled(string versionImpact) => $"Version impact of {versionImpact} cannot be handled";
}
