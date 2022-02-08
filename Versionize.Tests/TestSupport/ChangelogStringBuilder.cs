using System.Text;

namespace Versionize.Tests.TestSupport;

public class ChangelogStringBuilder
{
    private readonly StringBuilder _sb = new();

    public ChangelogStringBuilder Append(string text, int lineBreaks = 1)
    {
        _sb.Append(text);
        for (int i = 0; i < lineBreaks; i++)
        {
            _sb.Append('\n');
        }
        return this;
    }

    public string Build() => _sb.ToString();
}
