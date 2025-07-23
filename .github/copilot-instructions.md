# Copilot Instructions for Versionize

## Project Overview
Versionize is a .NET command-line tool for automatic versioning and CHANGELOG generation using conventional commit messages. It's built with C# and targets multiple .NET versions (7.0, 8.0, 9.0).

## Code Style and Conventions

### Language Features
- **C# Version**: Latest language version with implicit usings enabled
- **Nullable Reference Types**: Enabled project-wide (`<Nullable>enable</Nullable>`)
- **File-Scoped Namespaces**: Required (enforced via .editorconfig)
- **ImplicitUsings**: Enabled

### Naming Conventions
- **Classes**: PascalCase, sealed when appropriate
- **Methods**: PascalCase
- **Properties**: PascalCase with `{ get; set; }` or `{ get; init; }` for immutable properties
- **Fields**: camelCase with underscore prefix for private fields (e.g., `_testSetup`)
- **Constants**: PascalCase
- **Local variables**: camelCase
- **Parameters**: camelCase

### Class Design Patterns
- **Prefer sealed classes** unless inheritance is explicitly needed
- **Use static classes** for utility methods (e.g., `ConventionalCommitParser`, `TempProject`)
- **Nested Options classes** for complex method parameters with implicit operators for conversion
- **File-scoped namespaces** throughout the codebase

### Code Structure
```csharp
// Example class structure
namespace Versionize.ConventionalCommits;

public sealed class ConventionalCommitParser
{
    private static readonly string[] NoteKeywords = ["BREAKING CHANGE"];
    private const string DefaultHeaderPattern = "...";

    public static ConventionalCommit Parse(Commit commit, CommitParserOptions? options)
    {
        // Implementation
    }
}
```

### Options Pattern
Use nested Options classes with implicit operators for parameter passing:

```csharp
public sealed class VersionCalculator
{
    public static SemanticVersion Bump(Options options, ...)
    {
        // Implementation
    }

    public sealed class Options
    {
        public bool IgnoreInsignificantCommits { get; init; }
        public string? ReleaseAs { get; init; }

        public static implicit operator Options(VersionizeOptions versionizeOptions)
        {
            return new Options
            {
                IgnoreInsignificantCommits = versionizeOptions.IgnoreInsignificantCommits,
                ReleaseAs = versionizeOptions.ReleaseAs,
            };
        }
    }
}
```

## Dependencies and Libraries

### Main Dependencies
- **LibGit2Sharp** (0.30.0): Git operations
- **McMaster.Extensions.CommandLineUtils** (4.1.1): CLI framework
- **NuGet.Versioning** (6.11.0): Semantic versioning
- **System.Text.Json** (8.0.5): JSON serialization

### Test Dependencies
- **xUnit** (2.9.2): Testing framework
- **Shouldly** (4.2.1): Fluent assertions
- **Microsoft.NET.Test.Sdk** (17.11.1): Test SDK

## Testing Patterns

### Test Class Structure
```csharp
public class ComponentTests : IDisposable
{
    private readonly TestSetup _testSetup;
    private readonly TestPlatformAbstractions _testPlatformAbstractions;

    public ComponentTests()
    {
        _testSetup = TestSetup.Create();
        _testPlatformAbstractions = new TestPlatformAbstractions();
        CommandLineUI.Platform = _testPlatformAbstractions;
    }

    [Fact]
    public void ShouldPerformExpectedBehavior()
    {
        // Arrange
        // Act
        // Assert
    }

    public void Dispose()
    {
        _testSetup.Dispose();
    }
}
```

### Test Naming
- Test methods: `ShouldPerformExpectedBehavior` pattern
- Use descriptive names that explain the expected behavior
- Prefer `[Fact]` for simple tests, `[Theory]` with `[InlineData]` for parameterized tests

### Assertions
Use **Shouldly** for fluent assertions:
```csharp
result.ShouldBe(expected);
collection.ShouldContain(item);
file.ShouldExist();
changelogContents.ShouldContain("### Features", Case.Sensitive);
```

### Test Data Patterns
```csharp
[Theory]
[InlineData("feat: new feature", "feat", "new feature")]
[InlineData("fix(scope): bug fix", "fix", "bug fix")]
public void ShouldParseCommitMessage(string message, string expectedType, string expectedSubject)
{
    // Test implementation
}
```

## Project Structure Patterns

### Namespace Organization
- `Versionize` - Core types and main program
- `Versionize.Config` - Configuration classes and options
- `Versionize.CommandLine` - CLI utilities and abstractions
- `Versionize.ConventionalCommits` - Commit parsing logic
- `Versionize.Lifecycle` - Core workflow components
- `Versionize.Versioning` - Version calculation and manipulation
- `Versionize.Git` - Git repository extensions
- `Versionize.Changelog` - Changelog generation
- `Versionize.BumpFiles` - File version bumping

### Error Handling
- Use `CommandLineUI.Exit()` for user-facing errors with meaningful messages
- Custom exceptions for domain-specific errors (e.g., `InvalidPrereleaseIdentifierException`)
- Validation at method entry points

## Common Patterns

### Static Imports
Limited use of static imports, primarily for UI utilities:
```csharp
using static Versionize.CommandLine.CommandLineUI;
```

### String Interpolation
Use string interpolation for formatted strings:
```csharp
Exit($"Version was not affected by commits since last release ({version})", exitCode);
```

### Collection Initialization
Use collection expressions where available:
```csharp
public List<ConventionalCommitNote> Notes { get; set; } = [];
private static readonly string[] NoteKeywords = ["BREAKING CHANGE"];
```

### Null Handling
- Use nullable reference types consistently
- Prefer `string?` for optional string parameters
- Use null-conditional operators (`?.`) and null-coalescing (`??`) appropriately

## Configuration Patterns

### Options Classes
- Use `required` for mandatory properties
- Use `init` for immutable properties where appropriate
- Provide sensible defaults using static properties

### JSON Configuration
- Support both CLI arguments and JSON configuration files
- Use `System.Text.Json` for serialization
- Merge CLI and file configurations with CLI taking precedence

## Git Integration Patterns

### Repository Operations
- Use LibGit2Sharp for all Git operations
- Implement extension methods in `RepositoryExtensions` for domain-specific operations
- Handle Git errors gracefully with user-friendly messages

### Commit Processing
- Parse commits using conventional commit standards
- Support configurable regex patterns for different commit formats
- Extract metadata (issues, breaking changes) from commit messages

## Changelog Generation

### Template-Based Approach
- Use configurable sections for different commit types
- Support hiding sections and custom section names
- Generate markdown with proper anchors and formatting
- Support multiple link builders (GitHub, GitLab, Azure, etc.)

When contributing to this project, follow these established patterns and conventions to maintain consistency with the existing codebase.
