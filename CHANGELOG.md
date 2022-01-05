# Change Log

All notable changes to this project will be documented in this file. See [versionize](https://github.com/saintedlama/versionize) for commit guidelines.

<a name="1.10.0"></a>
## [1.10.0](https://www.github.com/saintedlama/versionize/releases/tag/v1.10.0) (2022-1-5)

### Features

* add Gitlab link builder (#38) ([94d79cc](https://www.github.com/saintedlama/versionize/commit/94d79ccbb2835df03bd693e5d1daeb6fd4fa8244))
* implement a bitbucket link builder (#39) ([60df2da](https://www.github.com/saintedlama/versionize/commit/60df2dab83a96afd58343b1aeec4a86b43f88465))
* update dependencies including LibGit2Sharp (#40) ([c02ba81](https://www.github.com/saintedlama/versionize/commit/c02ba8160eeb8bc2f005d95921f29ac622b825c7))
* update libgit2sharp (#37) ([f5b8a08](https://www.github.com/saintedlama/versionize/commit/f5b8a0877592f67d85f6f7c3756ef0ca841cbcdd))

<a name="1.9.0"></a>
## [1.9.0](https://www.github.com/saintedlama/versionize/releases/tag/v1.9.0) (2021-12-30)

### Bug Fixes

* changelog includes literal line break characters (#35) ([b0283b3](https://www.github.com/saintedlama/versionize/commit/b0283b3276e5c276ad97a44b7c49867473d96784))
* invalid url exception (#36) ([e3db063](https://www.github.com/saintedlama/versionize/commit/e3db0632b0f96f240d4c08df96840d4d85d73622))

### Features

* deprecate netcoreapp2.1 and support net6.0 target frameworks (#33) ([e2fbf39](https://www.github.com/saintedlama/versionize/commit/e2fbf39903d94e625070a3ac78444132453ea48c))

<a name="1.8.0"></a>
## [1.8.0](https://www.github.com/saintedlama/versionize/releases/tag/v1.8.0) (2021-10-5)

### Features

* support configuration via .versionize file in json format (#32) ([401cbce](https://www.github.com/saintedlama/versionize/commit/401cbce1f9d4c4cbe6a366cb7fd8ccd8214b4699))

<a name="1.7.0"></a>
## [1.7.0](https://www.github.com/saintedlama/versionize/releases/tag/v1.7.0) (2021-10-4)

### Features

* add azure links (#22) ([5ffca1a](https://www.github.com/saintedlama/versionize/commit/5ffca1a97475a9d7a9ff2763705d02cec680067f))
* add release commit message suffix option (#25) ([3598a25](https://www.github.com/saintedlama/versionize/commit/3598a258818f88c4a7b997a3269f17e76850e956))
* update dependencies (#31) ([aafaf2f](https://www.github.com/saintedlama/versionize/commit/aafaf2f247c7ed047641b15ef674be6c5f407b94))
* update LibGit2Sharp ([9290f66](https://www.github.com/saintedlama/versionize/commit/9290f66e3084983e1f8ebb1d492cc24493fc357c))

<a name="1.6.2"></a>
## [1.6.2](https://www.github.com/saintedlama/versionize/releases/tag/v1.6.2) (2021-1-9)

### Bug Fixes

* use preview version 0.27.0-preview-0096 to circumvent Ubuntu 20.04.1 issues (#19) ([f6ac24e](https://www.github.com/saintedlama/versionize/commit/f6ac24e5e6d41588fdaa79f9441a5ebb574eff0d))

<a name="1.6.1"></a>
## [1.6.1](https://www.github.com/saintedlama/versionize/releases/tag/v1.6.1) (2020-11-29)

### Bug Fixes

* use friendly name of tag instead of annotation to support projects using lightweight tags ([1d07076](https://www.github.com/saintedlama/versionize/commit/1d070765f26966370650039b4cefd138ef193f06))

<a name="1.6.0"></a>
## 1.6.0 (2020-11-29)

### Features

* support dotnet 5
* add github links to changelog (#12)
* Write change with scope (#10)

<a name="1.5.1"></a>
## 1.5.1 (2020-8-16)

### Bug Fixes

* fix changelog link generation issues to support appending to existing changelogs

<a name="1.5.0"></a>
## 1.5.0 (2020-8-16)

### Features

* add option to include all commit in a changelog

## 1.4.0 (2020-8-11)

### Features

* Added parameter to ignore insignificant commits (#8)

## 1.3.0 (2020-1-22)

## 1.2.0 (2018-12-18)

### Features

* add skip-commit option to not commit changes made to changelog and project
* add command line option --silent to supress cli output

## 1.1.0 (2018-10-5)

### Features

* add version option to cli

## 1.0.0 (2018-9-29)

### Bug Fixes

* add less space aftre blocks
* correctly remove the preamble
* append changelog block if NOT empty
* prepend preamble

### Features

* add options to release a specifc version, prepare for ci and increase test coverage
* implement changelog and the like

