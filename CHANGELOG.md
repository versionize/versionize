# Change Log

All notable changes to this project will be documented in this file. See [versionize](https://github.com/saintedlama/versionize) for commit guidelines.

<a name="1.6.3"></a>
## [1.6.3](https://www.github.com/blyzer/versionize/releases/tag/v1.6.3) (2021-1-14)

### Other

* **Test:** adding text azure to the projecte solution ([0b3fcd7](https://www.github.com/blyzer/versionize/commit/0b3fcd796483ea19ade3fda76a9f9a4ae764115c))
* **Test:** renaming method test ([7c6f2cb](https://www.github.com/blyzer/versionize/commit/7c6f2cb593c80ffea7c2fd21dfb66722d2c94adc))
* **Test:** renaming test ([1c0a9c5](https://www.github.com/blyzer/versionize/commit/1c0a9c529dffccbd2cad5fb593ecb5bfe0afc168))
* **Test:** wrong name changelog link builder ([8909138](https://www.github.com/blyzer/versionize/commit/89091382ec37761d707e8e8125f89fe21e0401ef))

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

