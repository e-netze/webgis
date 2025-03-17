using System;
using System.Xml;

namespace E.Standard.WebMapping.GeoServices.AXL;

public class Globals
{
    public Globals()
    {
        // 
        // TODO: Fügen Sie hier die Konstruktorlogik hinzu
        //
    }
    static public string getHotlinkUrl(XmlNode feature, string url)
    {
        int pos1 = 0, pos2;
        pos1 = url.IndexOf("[");
        while (pos1 != -1)
        {
            pos2 = url.IndexOf("]", pos1);
            if (pos2 == -1)
            {
                break;
            }

            string field = url.Substring(pos1 + 1, pos2 - pos1 - 1);
            url = url.Replace("[" + field + "]", getFieldValue(feature, field).Replace(" ", "%20"));
            pos1 = url.IndexOf("[");
        }
        return url;
    }

    static public string[] getKeyParameters(string commandLine)
    {
        int pos1 = 0, pos2;
        pos1 = commandLine.IndexOf("[");
        string parameters = "";

        while (pos1 != -1)
        {
            pos2 = commandLine.IndexOf("]", pos1);
            if (pos2 == -1)
            {
                break;
            }

            if (parameters != "")
            {
                parameters += ";";
            }

            parameters += commandLine.Substring(pos1 + 1, pos2 - pos1 - 1);
            pos1 = commandLine.IndexOf("[", pos2);
        }
        if (parameters != "")
        {
            return parameters.Split(';');
        }
        else
        {
            return null;
        }
    }

    #region ParseParameter
    static public string ParseParameter(string parameter, string val)
    {
        string[] operations = parameter.Split('.');
        if (operations.Length == 1)
        {
            return val;
        }

        for (int i = 1; i < operations.Length; i++)
        {
            val = PerformOperation(operations[i], val);
        }
        return val;
    }
    static private string PerformOperation(string operation, string val)
    {
        switch (operation.ToLower())
        {
            case "filename":
                return getFilename(val);
            case "filetitle":
                return getFiletitle(val);
        }

        return val;
    }
    static private string getFilename(string fullname)
    {
        int pos = fullname.Replace(@"\", "/").LastIndexOf("/");
        if (pos == -1)
        {
            return fullname;
        }

        return fullname.Substring(pos + 1, fullname.Length - pos - 1);
    }
    static private string getFiletitle(string fullname)
    {
        fullname = getFilename(fullname);
        int pos = fullname.LastIndexOf(".");
        if (pos == -1)
        {
            return fullname;
        }

        return fullname.Substring(0, pos);
    }
    #endregion

    static public string getCommandLine(string line, string[] keyParameters, string[] keyValues)
    {
        if (keyParameters == null || keyValues == null)
        {
            return line;
        }

        int count = Math.Min(keyParameters.Length, keyValues.Length);
        for (int i = 0; i < count; i++)
        {
            line = line.Replace("[" + keyParameters[i] + "]", keyValues[i].Replace("\\", "/"));
        }
        return line;
    }

    static public void parseQueryFieldAndValue(ref string Field, ref string Value)
    {
        if (Field.IndexOf("(") != -1 && Field.IndexOf(")") != -1)
        {
            int pos = Field.IndexOf(")");
            string Case = Field.Substring(0, pos + 1);
            Field = Field.Substring(pos + 1, Field.Length - pos - 1);

            switch (Case.ToLower())
            {
                case "(upper)":
                    Value = Value.ToUpper();
                    break;
                case "(lower)":
                    Value = Value.ToLower();
                    break;
            }
        }
    }

    static public string getFieldValue(XmlNode feature, string name)
    {
        XmlNodeList fields = feature.SelectNodes("FIELDS/FIELD");

        if (name.IndexOf("{") != 0)
        {
            name = name.ToUpper();
            string shortname = shortName(name);
            foreach (XmlNode field in fields)
            {
                string fieldname = field.Attributes["name"].Value.ToString().ToUpper();
                int index = fieldname.IndexOf("." + name);
                int pos = fieldname.Length - name.Length - 1;
                if (fieldname == name ||
                    (index == -1 && fieldname == shortname) ||
                    (index != -1 && index == pos))
                {
                    string val = field.Attributes["value"].Value.ToString();
                    return val;
                }
            }
        }
        else
        {
            XmlNode envelope = feature.SelectSingleNode("ENVELOPE");
            if (envelope != null)
            {
                if (envelope.Attributes["minx"] != null &&
                    envelope.Attributes["miny"] != null &&
                    envelope.Attributes["maxx"] != null &&
                    envelope.Attributes["maxy"] != null)
                {
                    double minx = Convert.ToDouble(envelope.Attributes["minx"].Value.Replace(".", ","));
                    double miny = Convert.ToDouble(envelope.Attributes["miny"].Value.Replace(".", ","));
                    double maxx = Convert.ToDouble(envelope.Attributes["maxx"].Value.Replace(".", ","));
                    double maxy = Convert.ToDouble(envelope.Attributes["maxy"].Value.Replace(".", ","));
                    switch (name)
                    {
                        case "{X}":
                            return ((minx + maxx) * 0.5).ToString();
                        case "{Y}":
                            return ((miny + maxy) * 0.5).ToString();
                        case "{MINX}":
                            return minx.ToString();
                        case "{MINY}":
                            return miny.ToString();
                        case "{MAXX}":
                            return maxx.ToString();
                        case "{MAXY}":
                            return maxy.ToString();

                        case "{x}":
                            return ((minx + maxx) * 0.5).ToString().Replace(",", ".");
                        case "{y}":
                            return ((minx + maxx) * 0.5).ToString().Replace(",", ".");
                        case "{minx}":
                            return minx.ToString().Replace(",", ".");
                        case "{miny}":
                            return miny.ToString().Replace(",", ".");
                        case "{maxx}":
                            return maxx.ToString().Replace(",", ".");
                        case "{maxy}":
                            return maxy.ToString().Replace(",", ".");
                    }
                }
            }
        }
        return "";
    }

    static public string shortName(string fieldname)
    {
        int pos = 0;
        string[] fieldnames = fieldname.Split(';');
        fieldname = "";
        for (int i = 0; i < fieldnames.Length; i++)
        {
            while ((pos = fieldnames[i].IndexOf(".")) != -1)
            {
                fieldnames[i] = fieldnames[i].Substring(pos + 1, fieldnames[i].Length - pos - 1);
            }
            if (fieldname != "")
            {
                fieldname += ";";
            }

            fieldname += fieldnames[i];
        }

        return fieldname;
    }

    static public string add2Filename(string filename, string add)
    {
        int pos = -1;
        while (filename.IndexOf(".", pos + 1) != -1)
        {
            pos = filename.IndexOf(".", pos + 1);
        }
        if (pos == -1)
        {
            return filename + add;
        }

        if (pos == 0)
        {
            return add + filename;
        }

        return filename.Substring(0, pos) + add + filename.Substring(pos, filename.Length - pos);
    }
}
