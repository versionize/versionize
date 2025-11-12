Let's brainstorm a new high level architcture. I'd like to refactor WorkingCopy.Versionize to a clean fluent builder pipeline-like api. Something like

```csharp
return VersionizePipeline.Create(versionizeOptions)
    .ReadVersion()
    .GetCommits()
    .BumpVersion()
    .UpdateChangelog()
    .CreateCommit()
    .CreateTag();
```

where all the messy state/arg passing are hidden behind an easily understandable high level api. Each step should return an immutable instance of whatever the type of the next step is e.g. ReadVersion[Context|Step], BumpVersion[Context|Step], where each context/step contains a single public parameterless method. I also plan to utilize Microsoft.Extensions.DependencyInjection for easier testing and mocking (and maybe migrating some of the static classes to be non-static and implement an interface). And Program.cs would eventually be refactored to resemble something like

```csharp
public static int Main(string[] args)
{
    var services = new ServiceCollection()
        .AddSingleton<IMyService, MyServiceImplementation>()
        .AddSingleton<IConsole>(PhysicalConsole.Singleton)
        .BuildServiceProvider();

    var app = new CommandLineApplication<Program>();
    app.Conventions
        .UseDefaultConventions()
        .UseConstructorInjection(services);
    return app.Execute(args);
}

private readonly IMyService _myService;

public Program(IMyService myService)
{
    _myService = myService;
}
```

And subcommands could possibly be their own class e.g. class ChangelogCommand and referenced via the Subcommand attribute. The subcommands would have their own fluent builder api too.

And the Program.GetWorkingCopy and WorkingCopy.Discover would probably be better as a step shared by the pipelines.