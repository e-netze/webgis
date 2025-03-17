using System.Text;

namespace E.Standard.Extensions.Text;

static public class StringBuilderExtensions
{
    static public void Add(this StringBuilder sb, params string[] values)
    {
        if (values == null)
        {
            return;
        }

        foreach (var value in values)
        {
            sb.Append(value);
        }
    }

    static public void Add(this StringBuilder sb, params object[] values)
    {
        if (values == null)
        {
            return;
        }

        foreach (var value in values)
        {
            sb.Append(value);
        }
    }
}
