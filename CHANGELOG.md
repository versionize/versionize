# Change Log

All notable changes to this project will be documented in this file. See [versionize](https://github.com/versionize/versionize) for commit guidelines.

<a name="1.16.0"></a>
## [1.16.0](https://www.github.com/versionize/versionize/releases/tag/v1.16.0) (2022-12-23)

### Features

* add net7 support ([39960e9](https://www.github.com/versionize/versionize/commit/39960e988b97d3277be2d7370b51316f485f62b3))

<a name="1.15.2"></a>
## [1.15.2](https://www.github.com/versionize/versionize/releases/tag/v1.15.2) (2022-10-4)

### Bug Fixes

* throws error when protected folder is encountered (#83) ([2f348dc](https://www.github.com/versionize/versionize/commit/2f348dc91e1a876ac289b1a780387ab791d045c1))

<a name="1.15.1"></a>
## [1.15.1](https://www.github.com/versionize/versionize/releases/tag/v1.15.1) (2022-10-1)

### Bug Fixes

* bump message shows same version for prev and next Issue #78  (#81) ([59e4d76](https://www.github.com/versionize/versionize/commit/59e4d767f39c68e34c66bec1b9369794ef8bab7f))

<a name="1.15.0"></a>
## [1.15.0](https://www.github.com/versionize/versionize/releases/tag/v1.15.0) (2022-6-26)

### Features

* add support for aggregating prereleases in changelog (#77) ([dae45f1](https://www.github.com/versionize/versionize/commit/dae45f1d38bc75e3b4ce9856d9463b7c00a89e46))

<a name="1.14.0"></a>
## [1.14.0](https://www.github.com/versionize/versionize/releases/tag/v1.14.0) (2022-5-13)

### Features

* exclamation mark to signifying breaking ([74576d4](https://www.github.com/versionize/versionize/commit/74576d4b5da02130380d8c367052ebd8aa9a5910))

### Other

* configuring gitpod (#68) ([7f59f11](https://www.github.com/versionize/versionize/commit/7f59f119ec11eb5f00f492da03bcb92b57a49b31))
* exit publishing workflow gracefully if no release is required (#70) ([c130107](https://www.github.com/versionize/versionize/commit/c1301077e971fabfda9ffa0c566ae0e1d7747959))

<a name="1.13.0"></a>
## [1.13.0](https://www.github.com/versionize/versionize/releases/tag/v1.13.0) (2022-3-13)

### Features

* support vbproj (#69) ([7e54235](https://www.github.com/versionize/versionize/commit/7e54235df2706af4b044dda3a3b75207bd075b2b))

<a name="1.12.1"></a>
## [1.12.1](https://www.github.com/versionize/versionize/releases/tag/v1.12.1) (2022-3-5)

### Bug Fixes

* check if version tag exists (#64) ([1627983](https://www.github.com/versionize/versionize/commit/16279838b3b0d935ef639a25fedc7426a606d89e))

### Other

* automatic nuget releases from github (#63) ([25472b2](https://www.github.com/versionize/versionize/commit/25472b2850e95e30525fb2ea4868e921af4fcb54))
* include git push in release cmd (#66) ([e985c68](https://www.github.com/versionize/versionize/commit/e985c68f13213a33600aa68e345f3ac971aa23bc))
* test some edge cases (#67) ([bd2e1e1](https://www.github.com/versionize/versionize/commit/bd2e1e1620be89edef231975db59aec06d0ac793))
* use versionize org not saintedlama org (#65) ([8e7b369](https://www.github.com/versionize/versionize/commit/8e7b369e0b3872e84e0b97a2fc984acd4ab9377f))

<a name="1.12.0"></a>
## [1.12.0](https://www.github.com/versionize/versionize/releases/tag/v1.12.0) (2022-2-26)

### Features

* add option to exit on insignificant commits (#58) ([1819c1c](https://www.github.com/versionize/versionize/commit/1819c1cb0c5cd3e5831548eb9b1bd64879f10688))
* support fsharp projects (#55) ([488ce68](https://www.github.com/versionize/versionize/commit/488ce6862c7986a257512c3ea8cc0df67ef4b089))

### Bug Fixes

* console colors and removed coloring complexity (#57) ([ca217d2](https://www.github.com/versionize/versionize/commit/ca217d25e3e72d5f420f875c678d83ebad7066c5))
* detect versions in xml namespaced projects (#62) ([7102a27](https://www.github.com/versionize/versionize/commit/7102a27e651619d21902557fe81cb7ecf2ec3f5b))

<a name="1.11.0"></a>
## [1.11.0](https://www.github.com/versionize/versionize/releases/tag/v1.11.0) (2022-2-11)

### Features

* add inspect command (#41) ([5e0d464](https://www.github.com/versionize/versionize/commit/5e0d464060bb892637c0176d5ea5de2ec7fdb6be))
* print changelog diff during a dry-run (#45) ([40a58f3](https://www.github.com/versionize/versionize/commit/40a58f3de4aabc0b214309b4de36554031cf7903))
* print help on NotFoundException (#44) ([7cc1ddc](https://www.github.com/versionize/versionize/commit/7cc1ddcf7ad2258b93976b4c4b85dc29f80f5228))
* support changelog customization options (#43) ([c27cf07](https://www.github.com/versionize/versionize/commit/c27cf072ffc91547d2953220df1a469ae3ff10d1))
* support pre-releases  (#50) ([2aa68c7](https://www.github.com/versionize/versionize/commit/2aa68c743ceece5e909c27157fa50462405b7830))
* warn about missing git configs (#53) ([5782999](https://www.github.com/versionize/versionize/commit/5782999c4967fd408c44ced1bffad697b22ba117))

<a name="1.10.0"></a>
## [1.10.0](https://www.github.com/versionize/versionize/releases/tag/v1.10.0) (2022-1-5)

### Features

* add Gitlab link builder (#38) ([94d79cc](https://www.github.com/versionize/versionize/commit/94d79ccbb2835df03bd693e5d1daeb6fd4fa8244))
* implement a bitbucket link builder (#39) ([60df2da](https://www.github.com/versionize/versionize/commit/60df2dab83a96afd58343b1aeec4a86b43f88465))
* update dependencies including LibGit2Sharp (#40) ([c02ba81](https://www.github.com/versionize/versionize/commit/c02ba8160eeb8bc2f005d95921f29ac622b825c7))
* update libgit2sharp (#37) ([f5b8a08](https://www.github.com/versionize/versionize/commit/f5b8a0877592f67d85f6f7c3756ef0ca841cbcdd))

<a name="1.9.0"></a>
## [1.9.0](https://www.github.com/versionize/versionize/releases/tag/v1.9.0) (2021-12-30)

### Bug Fixes

* changelog includes literal line break characters (#35) ([b0283b3](https://www.github.com/versionize/versionize/commit/b0283b3276e5c276ad97a44b7c49867473d96784))
* invalid url exception (#36) ([e3db063](https://www.github.com/versionize/versionize/commit/e3db0632b0f96f240d4c08df96840d4d85d73622))

### Features

* deprecate netcoreapp2.1 and support net6.0 target frameworks (#33) ([e2fbf39](https://www.github.com/versionize/versionize/commit/e2fbf39903d94e625070a3ac78444132453ea48c))

<a name="1.8.0"></a>
## [1.8.0](https://www.github.com/versionize/versionize/releases/tag/v1.8.0) (2021-10-5)

### Features

* support configuration via .versionize file in json format (#32) ([401cbce](https://www.github.com/versionize/versionize/commit/401cbce1f9d4c4cbe6a366cb7fd8ccd8214b4699))

<a name="1.7.0"></a>
## [1.7.0](https://www.github.com/versionize/versionize/releases/tag/v1.7.0) (2021-10-4)

### Features

* add azure links (#22) ([5ffca1a](https://www.github.com/versionize/versionize/commit/5ffca1a97475a9d7a9ff2763705d02cec680067f))
* add release commit message suffix option (#25) ([3598a25](https://www.github.com/versionize/versionize/commit/3598a258818f88c4a7b997a3269f17e76850e956))
* update dependencies (#31) ([aafaf2f](https://www.github.com/versionize/versionize/commit/aafaf2f247c7ed047641b15ef674be6c5f407b94))
* update LibGit2Sharp ([9290f66](https://www.github.com/versionize/versionize/commit/9290f66e3084983e1f8ebb1d492cc24493fc357c))

<a name="1.6.2"></a>
## [1.6.2](https://www.github.com/versionize/versionize/releases/tag/v1.6.2) (2021-1-9)

### Bug Fixes

* use preview version 0.27.0-preview-0096 to circumvent Ubuntu 20.04.1 issues (#19) ([f6ac24e](https://www.github.com/versionize/versionize/commit/f6ac24e5e6d41588fdaa79f9441a5ebb574eff0d))

<a name="1.6.1"></a>
## [1.6.1](https://www.github.com/versionize/versionize/releases/tag/v1.6.1) (2020-11-29)

### Bug Fixes

* use friendly name of tag instead of annotation to support projects using lightweight tags ([1d07076](https://www.github.com/versionize/versionize/commit/1d070765f26966370650039b4cefd138ef193f06))

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
