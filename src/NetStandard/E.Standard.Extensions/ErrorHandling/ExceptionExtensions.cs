using E.Standard.Extensions.Abstractions;
using System;
using System.IO;
using System.Text;

namespace E.Standard.Extensions.ErrorHandling;

static public class ExceptionExtensions
{
    static public string FullMessage(this Exception exception,
                                     string indentationString = " >> ",
                                     bool appendTypes = true)
    {
        if (exception == null)
        {
            return String.Empty;
        }

        var sb = new StringBuilder();

        int level = 0;
        while (exception != null)
        {
            if (sb.Length > 0)
            {
                sb.Append(Environment.NewLine);
            }

            if (appendTypes)
            {
                if (sb.Length > 0 && String.IsNullOrEmpty(indentationString))
                {
                    sb.Append(new string('-', 60));
                    sb.Append(Environment.NewLine);
                }
                string typeHeading = $"{exception.GetType().Name.AddIndentation(indentationString, level)}:";
                sb.Append(typeHeading);
                sb.Append(Environment.NewLine);
            }

            sb.Append(exception.Message.RemoveSecrets().AddIndentation(indentationString, level++));

            exception = exception.InnerException;
        }

        return sb.ToString();
    }

    static public Exception ToFullExceptionSummary(this Exception exception)
    {
        var message = exception.FullMessage(indentationString: String.Empty, appendTypes: true);

        return new Exception(message);
    }

    static private string RemoveSecrets(this string message)
    {
        // ToDo:

        return message;
    }

    static public string AddIndentation(this string message, string indentationString, int level)
    {
        if (level == 0 || String.IsNullOrEmpty(indentationString))
        {
            return message;
        }

        StringBuilder sb = new StringBuilder();

        for (int i = 0; i < level; i++)
        {
            sb.Append(indentationString);
        }


        sb.Append(message);

        return sb.ToString();
    }

    static public string SecureMessage(this Exception ex)
    {
        if (ex is IGenericExceptionMessage)
        {
            return ((IGenericExceptionMessage)ex).GenericMessage;
        }

        string message = ex.GetType() switch
        {
            Type t when t == typeof(FileNotFoundException) => "File Not Found",
            Type t when t == typeof(IOException) => "IO Exception",
            _ => ex.Message
        };

        // ToDo: Parse Messsage?

        return message;
    }
}
