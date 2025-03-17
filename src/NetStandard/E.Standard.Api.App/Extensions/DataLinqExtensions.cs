using System;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;

namespace E.Standard.Api.App.Extensions;

public enum StatementType
{
    Sql,
    Url
}
static public class DataLinqExtensions
{
    static public string ParseStatementPreCompilerDirectives(this string statement, NameValueCollection nvc, StatementType statementType = StatementType.Sql)
    {
        if (!statement.Contains("#if "))
        {
            return statement;
        }

        var stringReader = new StringReader(statement);

        StringBuilder sb = new StringBuilder();
        string statementLine;
        int level = 0;
        bool[] levelContition = new bool[100].Select(b => true).ToArray();

        while ((statementLine = stringReader.ReadLine()) != null)
        {
            statementLine = statementLine.Trim();
            if (statementLine.StartsWith("#if "))
            {
                level++;

                levelContition[level] = levelContition[level - 1];
                if (levelContition[level] == true)
                {
                    foreach (string parameter in statementLine.Substring(4).Trim().Split(','))
                    {
                        if (!nvc.AllKeys.Contains(parameter) || String.IsNullOrWhiteSpace(nvc[parameter]))
                        {
                            levelContition[level] = false;
                            break;
                        }
                    }
                }
            }
            else if (statementLine.StartsWith("#endif"))
            {
                level--;
                if (level < 0)
                {
                    throw new Exception("ParseStatement: Syntax error");
                }
            }
            else
            {
                if (levelContition[level] == true)
                {
                    switch (statementType)
                    {
                        case StatementType.Sql:
                            if (sb.Length > 0)
                            {
                                sb.Append(Environment.NewLine);
                            }

                            sb.Append(statementLine);
                            break;
                        case StatementType.Url:
                            if (!String.IsNullOrWhiteSpace(statementLine))
                            {
                                sb.Append(statementLine.Trim());
                            }

                            break;
                    }
                }
            }
        }

        if (level != 0)
        {
            throw new Exception("ParseStatement: Syntax error");
        }

        return sb.ToString();
    }

    static public string ToSingleLineStatement(this string statement)
    {
        if (!statement.Contains("\n"))
        {
            return statement;
        }

        var stringReader = new StringReader(statement);

        StringBuilder sb = new StringBuilder();
        string statementLine;

        while ((statementLine = stringReader.ReadLine()) != null)
        {
            statementLine = statementLine.Trim();

            if (!String.IsNullOrEmpty(statementLine))
            {
                if (sb.Length > 0)
                {
                    sb.Append(" ");
                }

                sb.Append(statementLine);
            }
        }

        return sb.ToString();
    }

    static public NameValueCollection RazorConstants(this XmlDocument config)
    {
        NameValueCollection ret = new NameValueCollection();

        if (config != null)
        {
            foreach (XmlNode constNode in config.SelectNodes("configuration/const/add[@name and @value]"))
            {
                ret[constNode.Attributes["name"].Value] = constNode.Attributes["value"].Value;
            }
        }

        return ret;
    }

    static public string ToCSharpConstants(this NameValueCollection constants, string className)
    {
        if (constants == null)
        {
            return String.Empty;
        }

        StringBuilder sb = new StringBuilder();

        sb.Append("var " + className + "=new {");
        sb.Append(Environment.NewLine);
        bool first = true;
        foreach (string name in constants.Keys)
        {
            //sb.Append("public const string " + name + "=\"" + constants[name] + "\";");
            if (!first)
            {
                sb.Append(", ");
            }

            sb.Append(name + "=\"" + constants[name] + "\"");
            first = false;
        }
        sb.Append("};");
        sb.Append(Environment.NewLine);

        return sb.ToString();
    }
}