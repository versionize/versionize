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
        var configFile = _cliConfig.ConfigurationFile.Value();
        string fileConfigPath;
        if (configFile != null && Path.IsPathRooted(configFile))
        {
            fileConfigPath = configFile;
        }
        else
        {
            var configDirectory = _cliConfig.ConfigurationDirectory.Value() ?? cwd;
            fileConfigPath = Path.Join(configDirectory, configFile ?? ".versionize");
        }

        var fileConfig = FileConfigLoader.LoadMerged(fileConfigPath);
        var mergedOptions = ConfigProvider.GetSelectedOptions(cwd, _cliConfig, fileConfig);
        return mergedOptions;
    }
}
