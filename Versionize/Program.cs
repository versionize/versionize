using System;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Reflection;
using McMaster.Extensions.CommandLineUtils;

namespace Versionize
{
    [Command(
        Name = "Versionize",
        Description = "Automatic versioning and CHANGELOG generation, using conventional commit messages")]
    class Program
    {
        public static int Main(string[] args)
        {
            var app = new CommandLineApplication();

            app.HelpOption();
            var optionSubject = app.Option("-s|--subject <SUBJECT>", "The subject", CommandOptionType.SingleValue);
            var optionRepeat = app.Option<int>("-n|--count <N>", "Repeat", CommandOptionType.SingleValue);

            app.OnExecute(() =>
            {
                WorkingCopy
                    .Discover(Directory.GetCurrentDirectory())
                    .Versionize();

                return 0;
            });

            return app.Execute(args);
        }
    }
}
