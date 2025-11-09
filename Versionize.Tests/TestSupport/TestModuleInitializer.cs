using System.Runtime.CompilerServices;
using LibGit2Sharp;

namespace Versionize;

public static class TestModuleInitializer
{
    [ModuleInitializer]
    public static void Init()
    {
        // Prevent LibGit2Sharp from using any global or system-level git config files
        LibGit2Sharp.GlobalSettings.SetConfigSearchPaths(ConfigurationLevel.Global, []);
        LibGit2Sharp.GlobalSettings.SetConfigSearchPaths(ConfigurationLevel.System, []);
        LibGit2Sharp.GlobalSettings.SetConfigSearchPaths(ConfigurationLevel.ProgramData, []);
        LibGit2Sharp.GlobalSettings.SetConfigSearchPaths(ConfigurationLevel.Xdg, []);
    }
}
