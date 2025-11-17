namespace Versionize.Pipeline;

public interface IPipelineStep<TIn, TOptions, TOut>
{
    TOut Execute(TIn input, TOptions options);
}

public interface IConvertibleFromVersionizeOptions<TSelf>
    where TSelf : IConvertibleFromVersionizeOptions<TSelf>
{
    static abstract TSelf FromVersionizeOptions(Config.VersionizeOptions options);
}
