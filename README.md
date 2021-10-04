# Versionize

[![AppVeyor build status](https://ci.appveyor.com/api/projects/status/r0rjv30llx7nhxl4?svg=true)](https://ci.appveyor.com/project/saintedlama/versionize)
[![Coverage Status](https://coveralls.io/repos/saintedlama/versionize/badge.svg?branch=)](https://coveralls.io/r/saintedlama/versionize?branch=master)
[![Conventional Commits](https://img.shields.io/badge/Conventional%20Commits-1.0.0-yellow.svg)](https://conventionalcommits.org)

> stop using weird build scripts to increment your nuget's version, use `versionize`!

Automatic versioning and CHANGELOG generation, using [conventional commit messages](https://conventionalcommits.org).

_how it works:_

1. when you land commits on your `master` branch, select the _Squash and Merge_ option (not required).
2. add a title and body that follows the [Conventional Commits Specification](https://conventionalcommits.org).
3. when you're ready to release a nuget package:
    1. `git checkout master; git pull origin master`
    2. run `versionize`
    3. `git push --follow-tags origin master`
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
Usage: versionize [options]

Options:
  -?|-h|--help                         Show help information
  -v|--version                         Show version information
  -w|--workingDir <WORKING_DIRECTORY>  Directory containing projects to version
  -d|--dry-run                         Skip changing versions in projects, changelog generation and git commit
  --skip-dirty                         Skip git dirty check
  -r|--release-as <VERSION>            Specify the release version manually
  --silent                             Suppress output to console
  --skip-commit                        Skip commit and git tag after updating changelog and incrementing the version
  -i|--ignore-insignificant-commits    Do not bump the version if no significant commits (fix, feat or BREAKING) are found
  --changelog-all                      Include all commits in the changelog not just fix, feat and breaking changes
  --commit-suffix                      Suffix to be added to the end of the release commit message (e.g. [skip ci])
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


## Developing

To get prettier test outputs run `dotnet test` with prettier test logger

```bash
dotnet test --logger prettier
```

## Roadmap

* [ ] Pre Releases to allow creating beta.1, beta.2 versions
* [ ] Support .versionrc like "standard-version" does
* [ ] Support mono repo joint and disjoint version strategies
* [x] ~~--silent command line switch to suppress commandline output~~
* [x] `-i`, `--ignore-insignificant-commits` command line switch to not create a new version if only insignificant (chore, ...) commits were done
* [x] GitHub URLs in changelog

