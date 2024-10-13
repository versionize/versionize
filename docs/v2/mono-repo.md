# Versionize and Mono repo
### requirement
- dotnet sdk (example using dotnet 8)
- versionize tool installed global/locally (example using global)
- terminal (example using powershell core 7) 
## User story:
As a .NET developer, you've likely seen projects structured with various C# libraries, console apps, and web applications. Sometimes, developers aim to manage everything within a single repository. With Versionize, developers can easily version their projects and generate changelogs, streamlining the management of your codebase.

### Scenario-example
**Task:** A project structure that includes a class library and a console application, each with its own versioning and changelog file.

Here's a PowerShell script that aggregates commands to create a folder structure, initialize a Git repository, and create .NET projects:
```powershell
cd ~ && mkdir mono && cd mono && git init && dotnet new gitignore && mkdir src && cd src && dotnet new console --name Console && dotnet new classlib --name Library
```
**Current structure of the project**
```
| .gitignore
\---src
    \---Console
    |   |   Console.csproj
    |   |   Program.cs
    \---Library
        |   Class1.cs
        |   Library.csproj
```
Versionize operates on the `<Version></Version>` tag in the `.csproj` file. To version the library and console application, add this tag to each `.csproj` file and initialize it with 0.0.0.
```xml
<!-- Console && Library files -->
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <Version>0.0.0</Version> <!-- Add this -->
  </PropertyGroup>

</Project>
```
When using a mono repo solution for multiple projects, a `.versionize` file is required for project configuration.  
Here is a command to create the file in the root project folder
```powershell
new-item ../.versionize
```
**Current structure of the project**
```
| .gitignore
| .versionize
\---src
    \---Console
    |   |   Console.csproj
    |   |   Program.cs
    \---Library
        |   Class1.cs
        |   Library.csproj
```
Its time to edit the `.versionize` file open your text editor and paste the code below
```json
{
    "skipDirty": true,
    "silent": false,
    "projects": [
        {
            "name": "library",
            "path": "src/Library",
            "tagTemplate": "{name}-v{version}",
            "changelog": {
                "header": "Library Changelog",
                "sections": [
                    {
                        "type": "feat",
                        "section": "✨ Features",
                        "hidden": false
                    },
                    {
                        "type": "fix",
                        "section": "🐛 Bug Fixes",
                        "hidden": false
                    },
                    {
                        "type": "perf",
                        "section": "🚀 Performance",
                        "hidden": false
                    }
                ]
            }
        },
        {
            "name": "console",
            "path": "src/Console",
            "tagTemplate": "{name}-v{version}",
            "changelog": {
                "header": "Console Changelog",
                "sections": [
                    {
                        "type": "feat",
                        "section": "✨ Features",
                        "hidden": false
                    },
                    {
                        "type": "fix",
                        "section": "🐛 Bug Fixes",
                        "hidden": false
                    },
                    {
                        "type": "perf",
                        "section": "🚀 Performance",
                        "hidden": false
                    }
                ]
            }
        }
    ]
}
```
> **Note:** This config file is mirror to `FileConfig.cs`

Another important point to note is that we have two `projects` in the projects node — one for the console application and one for the library. The paths will point to the respective folder structures, and we can also customize the tag template as needed.

Its time to work with git commands and versionize tool.  
```powershell
# add console project and commit
git add src/Console
git commit -m "feat: add console project"
# add .gitignore file and commit
git add .gitignore
git commit -m "chore: add gitignore file"
# add .versionize file and commit
git add .versionize
git commit -m "chore: add .versionze config file"
# its time to make version note that only first commit will be included in the changelog because its in the folder in src/Console and csproj file is there
versionize --proj-name "console"
# the command will give a tag of console-v0.0.0 and create changelog with only the feat commit (other commits not included)

# add library project and commit
git add src/Library
git commit -m "feat: add library project"

# show commits log until now 
# Note the hash will be different
git log --oneline 

# 518daa9 feat: add library project
# db693a2 (tag: console-v0.0.0) chore(release): 0.0.0
# b5fc6e4 chore: add .versionize config file
# 5666e9b chore: add .gitignore file
# 601aa70 feat: add console library

# Note the chore(release) tag made by the versionize tool
```
**Current structure of the project**
```
| .gitignore
| .versionize
\---src
    \---Console
    |   |   CHANGELOG.md
    |   |   Console.csproj
    |   |   Program.cs
    \---Library
        |   Class1.cs
        |   Library.csproj
```
New bug issues has been opened and we need to fix some bug in the projects

In Library project `Class1.cs`
```csharp
namespace Library;

public class Class1
{
    // add this 
    public void Fix1()
    {
        System.Console.WriteLine("add fix for console");
    }
}
```

In Console project `Program.cs`
```csharp
// See https://aka.ms/new-console-template for more information
Console.WriteLine("Hello, World!");
Console.WriteLine("Fixing this"); // add this
```

Open the terminal its time to commit and versioning the projects
```powershell
# add modified Program.cs file
git add src/Console
git commit -m "fix: new fix in console"
# add modified Class1.cs
git add src/Library
git commit -m "fix: new fix in library"
# make new version for console project now the version will be 0.0.1 and update the changelog file
versionize --proj-name "console"

# show updated commit log
git log --oneline

# e3fafbf (tag: console-v0.0.1) chore(release): 0.0.1
# d628fac fix: new fix in library
# 8d3f0d3 fix: new fix in console
# 518daa9 feat: add library project
# db693a2 (tag: console-v0.0.0) chore(release): 0.0.0
# b5fc6e4 chore: add .versionize config file
# 5666e9b chore: add .gitignore file
# 601aa70 feat: add console library

# Note the new chore(release) tag made by the versionize tool

# its time to release the library project it will be first version 0.0.0 and create new changelog file in the Library folder.
versionize --proj-name "library"

# Note this will add the feat commit (518daa9) and also the fix commit (d628fac) to the changelog

# show updated commit log
git log --oneline

# 82ec57f (HEAD -> main, tag: library-v0.0.0) chore(release): 0.0.0
# e3fafbf (tag: console-v0.0.1) chore(release): 0.0.1
# d628fac fix: new fix in library
# 8d3f0d3 fix: new fix in console
# 518daa9 feat: add library project
# db693a2 (tag: console-v0.0.0) chore(release): 0.0.0
# b5fc6e4 chore: add .versionize config file
# 5666e9b chore: add .gitignore file
# 601aa70 feat: add console library
```

**Current structure of the project**
```
| .gitignore
| .versionize
\---src
    \---Console
    |   |   CHANGELOG.md
    |   |   Console.csproj
    |   |   Program.cs
    \---Library
    |   |   CHANGELOG.md
        |   Class1.cs
        |   Library.csproj
```

In the end, with the Versionize tool, we'll have tags and logs for each project. This allows us to continue working, tagging, and logging our progress seamlessly and enjoyably.