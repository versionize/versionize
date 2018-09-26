# Versionize

[![Build Status](https://travis-ci.org/saintedlama/versionize.svg?branch=master)](https://travis-ci.org/saintedlama/versionize)

[![Coverage Status](https://coveralls.io/repos/saintedlama/versionize/badge.svg?branch=)](https://coveralls.io/r/saintedlama/versionize?branch=master)

[![Conventional Commits](https://img.shields.io/badge/Conventional%20Commits-1.0.0-yellow.svg)](https://conventionalcommits.org)

> stop using weird build scripts to increment your nuget's version, use `versionize`!

Automatic versioning and CHANGELOG generation, using [conventional commit messages](https://conventionalcommits.org).

_how it works:_

1. when you land commits on your `master` branch, select the _Squash and Merge_ option.
2. add a title and body that follows the [Conventional Commits Specification](https://conventionalcommits.org).
3. when you're ready to release a nuget package:
    1. `git checkout master; git pull origin master`
    2. run `versionize`
    3. `git push --follow-tags origin master`
    4. `dotnet pack`
    4. `dotnet nuget push`

`versionize` does the following:

1. bumps the version in your `.csproj` file (based on your commit history)
2. uses [conventional-changelog](https://github.com/conventional-changelog/conventional-changelog) to update _CHANGELOG.md_
3. commits `.csproj` file and _CHANGELOG.md_
4. tags a new release

## Installation

```bash
dotnet tool install --global Versionize
```