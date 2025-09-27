# Versionize

[![Coverage Status](https://coveralls.io/repos/versionize/versionize/badge.svg?branch=)](https://coveralls.io/r/versionize/versionize?branch=master)
[![Conventional Commits](https://img.shields.io/badge/Conventional%20Commits-1.0.0-yellow.svg)](https://conventionalcommits.org)

> stop using weird build scripts to increment your nuget's version, use `versionize`!

Automatic versioning and CHANGELOG generation, using [conventional commit messages](https://conventionalcommits.org).

_how it works:_

1. when you land commits on your `main` branch, select the _Squash and Merge_ option (not required).
2. add a title and body that follows the [Conventional Commits Specification](https://conventionalcommits.org).
3. when you're ready to release a nuget package:
    1. `git checkout main; git pull origin main`
    2. run `versionize`
    3. `git push --follow-tags origin main`
    4. `dotnet pack`
    5. `dotnet nuget push`

`versionize` does the following:

1. bumps the version in your `.csproj` file (based on your commit history)
2. uses [conventional-changelog](https://github.com/conventional-changelog/conventional-changelog) to update _CHANGELOG.md_
3. commits `.csproj` file and _CHANGELOG.md_
4. tags a new release

## Installation

```bash
dotnet tool install --global Versionize
```

## Usage

```bash
Usage: versionize [command] [options]

Options:
  -?|-h|--help                         Show help information.
  -v|--version                         Show version information.
  -w|--workingDir <WORKING_DIRECTORY>  Directory containing projects to version
  --configDir <CONFIG_DIRECTORY>       Directory containing the .versionize configuration file
  -d|--dry-run                         Skip changing versions in projects, changelog generation and git commit
  --skip-dirty                         Skip git dirty check
  -r|--release-as <VERSION>            Specify the release version manually
  --silent                             Suppress output to console
  --skip-commit                        Don't commit changes to the git repository
  --skip-tag                           Don't tag the release commit
  --omit-tag-version-prefix            Omit the 'v' prefix in git tags
  --skip-changelog                     Don't update the changelog
  -i|--ignore-insignificant-commits    Don't bump the version if no significant commits (fix, feat or BREAKING)
                                       are found
  --exit-insignificant-commits         Exits with a non zero exit code if no significant commits (fix, feat or
                                       BREAKING) are found
  --commit-suffix                      Suffix to be added to the end of the release commit message (e.g. [skip ci])
  -p|--pre-release                     Release as pre-release version with given pre release label.
  -a|--aggregate-pre-releases          Include all pre-release commits in the changelog since the last full version.
                                       Only applies when new version is stable (non pre-release).
  --find-release-commit-via-message    Use commit message instead of tag to find last release commit.
  --tag-only                           Don't read/write the version from/to project files. Depend on version tags only.
  --proj-name                          Name of a project defined in the configuration file (for monorepos)
  --first-parent-only-commits          Ignore commits beyond the first parent
  -s|--sign                            Sign the git commit and tag

Commands:
  inspect                              Prints the current version to stdout
  changelog                            Prints a given version's changelog to stdout
    -v|--version <VERSION>             The version to include in the changelog (defaults to latest version if not specified)
    -p|--preamble <PREAMBLE>           Text to display before the list of commits
```

## Supported commit types

Every commit should be in the form
`<type>[optional scope]: <description>`
for example
`fix(parser): remove colon from type and scope`

* fix - will trigger a patch version increment in the next release
* feat - will trigger a minor version increment in the next release
* all other types - you can use any commit type but that commit type will not trigger a version increment in the next release

Breaking changes **must** contain a line prefixed with `BREAKING CHANGE:` to allow versionize recognizing a breaking change. Breaking changes can use any commit type.

**Example**

```bash
git commit -m "chore: update dependencies" -m "BREAKING CHANGE: this will likely break the interface"
```

## Custom Commit Header Patterns

`versionize` supports custom commit header patterns for parsing commit messages. This is particularly useful when your team uses commit messages that differ from the conventional commits format.

To configure custom header patterns, create a `.versionize` file in your working directory with the following structure:

```json
{
  "CommitParser": {
    "HeaderPatterns": [
      "^Merged PR \\\\d+: (?<type>\\w*)(?:\\((?<scope>.*)\\))?(?<breakingChangeMarker>!)?: (?<subject>.*)$",
      "^Pull Request \\\\d+: (?<type>\\w*)(?:\\((?<scope>.*)\\))?(?<breakingChangeMarker>!)?: (?<subject>.*)$"
    ]
  }
}
```

### Example

If your team commits include messages like the following:

- `Merged PR 123: fix(squash-azure-case): subject text #64`
- `Pull Request 11792: feat(azure-case): subject text`

You can use the above configuration to ensure these commit messages are parsed correctly. `versionize` will extract the relevant type, scope, and subject information to include them in the changelog and determine version increments.


## The happy versioning walkthrough

### Preparation

Create a new project with the dotnet cli

```bash
mkdir SomeProject
dotnet new classlib
```

Ensure that a &lt;Version&gt; element is contained in file SomeProject.csproj

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <Version>1.0.0</Version>
  </PropertyGroup>
</Project>
```

### Using versionize

Now let's start committing and releasing

```bash
git init
...make some changes to "Class1.cs"
git add *
git commit -a -m "chore: initial commit"

versionize
```

Will add a CHANGELOG.md, add git tags and commit everything. Note that the version in `SomeProject.csproj` will not change since this is your first release with `versionize`.

```bash
...make some changes to "Class1.cs"
git commit -a -m "fix: something went wrong we need a bugfix release"

versionize
```

Will update CHANGELOG.md, add git tags and commit everything. Note that the version in `SomeProject.csproj` is now `1.0.1`.

```bash
...make some changes to "Class1.cs"
git commit -a -m "feat: something really awesome coming in the next release"

versionize
```

Will update CHANGELOG.md, add git tags and commit everything. Note that the version in `SomeProject.csproj` is now `1.1.0`.

```bash
...make some changes to "Class1.cs"
git commit -a -m "feat: a really cool new feature" -m "BREAKING CHANGE: the API will break. sorry"

versionize
```

Will update CHANGELOG.md, add git tags and commit everything. Note that the version in `SomeProject.csproj` is now `2.0.0` since
versionize detected a breaking change since the commit note `BREAKING CHANGE` was used above.

### Pre-releases

Versionize supports creating pre-release versions by using the `--pre-release` flag with a pre-release label, for example `alpha`.

The following workflow illustrates how pre-release workflows with versionize work.

```shell
> git commit -a -m "chore: initial commit"
> versionize
// Generates version v1.0.0

> git commit -a -m "feat: some feature"
> versionize --pre-release alpha
// Generates version v1.1.0-alpha.0

> git commit -a -m "feat: some additional feature"
> versionize --pre-release alpha
// Generates version v1.1.0-alpha.1

> git commit -a -m "feat: some breaking feature" -m "BREAKING CHANGE: This is a breaking change"
> versionize --pre-release alpha
// Generates version v2.0.0-alpha.0

> versionize
// Generates version v2.0.0
```

### Aggregated pre-releases changelog

By default, each commit message only appears in the release it was introduced. When using the pre-release feature
this can result in a fragmented changelog. For example, when promoting to a full release the user has to browse
through all the pre-release sections to see what's included.

```
v1.0.0-alpha.0
- featA
v1.0.0-alpha.1
- featB
v1.0.0
```

So to get around that you can pass the `--aggregate-pre-releases` flag

```
versionize --pre-release alpha
versionize --pre-release alpha
versionize --aggregate-pre-releases
```

to get output like the following

```
v1.0.0-alpha.0
- featA
v1.0.0-alpha.1
- featB
v1.0.0
- featA
- featB
```

This also works together with the `pre-release` option

`versionize --pre-release alpha --aggregate-pre-releases`

### Skip pre-release tags

Some developers may prefer not to tag pre-releases. Here's an example of how to achieve that:

```
versionize --pre-release alpha --skip-tag
versionize --pre-release alpha --skip-tag --find-release-commit-via-message
...
```

`find-release-commit-via-message` is necessary because Versionize uses git tags by default to determine the current version. Without a git tag, the way we determine which commits get included in the changelog is by searching for the last commit message that starts with _"chore(release):"_. 

## Configuration

You can configure `versionize` either by creating a `.versionize` JSON file the working directory.

Any of the command line parameters accepted by `versionize` can be provided via configuration file leaving out any `-`. For example `skip-dirty` can be provided as `skipDirty` in the configuration file. 
The `.versionize` configuration file is deserialized into a `FileConfig.cs` object behind the scenes.

Changelog customization can only be done via a `.versionize` file. The following is an example configuration:

```json
{
  "changelog": {
    "header": "My Changelog",
    "includeAllCommits": true,
    "sections": [
      {
        "type": "feat",
        "section": "✨ Features",
        "hidden": false
      },
      {
        "type": "fix",
        "section": "🐛 Bug Fixes",
        "hidden": true
      },
      {
        "type": "perf",
        "section": "🚀 Performance",
        "hidden": false
      }
    ]
  }
}
```

Because `IncludeAllCommits` is true and the _fix_ section is hidden, fix commits will appear in the a section titled "Other".

## Developing

Want to do a PR and not care about setting up your development environment?

[![Open in Gitpod](https://gitpod.io/button/open-in-gitpod.svg)](https://gitpod.io/#https://github.com/versionize/versionize)

To get prettier test outputs run `dotnet test` with prettier test logger

```bash
dotnet test --logger prettier
```

## Roadmap

* [] Signed release commits
* [] Command to print out changelog entry since last release (as opposed to writing to a file)
* [] Tag prefix config (for non mono-repos)
* [] Add support for bumping a Unity project version
* [] Improve documentation
