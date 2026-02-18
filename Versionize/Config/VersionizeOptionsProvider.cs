namespace Versionize.Config;

public interface IVersionizeOptionsProvider
{
    VersionizeOptions GetOptions();
}

public class VersionizeOptionsProvider(CliConfig _cliConfig) : IVersionizeOptionsProvider
{
    public VersionizeOptions GetOptions()
    {
        var cwd = _cliConfig.WorkingDirectory.Value() ?? Directory.GetCurrentDirectory();
        var configDirectory = _cliConfig.ConfigurationDirectory.Value() ?? cwd;
        var fileConfigPath = Path.Join(configDirectory, ".versionize");
        var fileConfig = FileConfigLoader.LoadMerged(fileConfigPath);
        var mergedOptions = ConfigProvider.GetSelectedOptions(cwd, _cliConfig, fileConfig);
        return mergedOptions;
    }
}
