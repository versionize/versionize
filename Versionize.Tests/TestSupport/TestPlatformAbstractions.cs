using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Colorful;
using Versionize.CommandLine;

namespace Versionize.Tests.TestSupport
{
    public class TestPlatformAbstractions : IPlatformAbstractions
    {
        public LogLevel Verbosity { get; set; }
        public List<FormatterMessage> Messages { get; } = new List<FormatterMessage>();

        public IEnumerable<Formatter[]> Formmatters
        {
            get
            {
                return Messages.Select(m => m.Formatters).Where(formatter => formatter != null).ToList();
            }
        }

        public void Exit(int exitCode)
        {
            throw new CommandLineExitException(exitCode);
        }

        public void WriteLine(string message, Color color)
        {
            Messages.Add(new FormatterMessage { Message = message });
        }

        public void WriteLineFormatted(string message, Color color, Formatter[] formatters)
        {
            Messages.Add(new FormatterMessage { Message = message, Formatters = formatters });
        }
    }

    public class FormatterMessage
    {
        public string Message { get; set; }
        public Formatter[] Formatters { get; set; }
    }
}
