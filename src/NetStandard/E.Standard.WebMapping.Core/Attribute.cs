using System;

namespace E.Standard.WebMapping.Core;

public class Attribute
{
    public Attribute()
    {
    }
    public Attribute(string name, string val)
    {
        Name = Alias = name;
        Value = val;
    }

    public Attribute(string name, string alias, string val)
    {
        Name = name;
        Alias = String.IsNullOrEmpty(alias) ? name : alias;
        Value = val;
    }

    public Attribute(string name, string val, Type dataType)
        : this(name, val)
    {
        DataType = dataType;
    }

    public Attribute(string name, string[] values)
    {
        Name = name;
        if (values != null)
        {
            Value = ArrayPrefix + String.Join(";", values);
        }
        else
        {
            Value = ArrayPrefix;
        }
    }

    public string Name { get; set; }
    public string Alias { get; }

    private string _value;
    virtual public string Value { get { return _value; } set { _value = value; } }
    public Type DataType = typeof(string);

    static public string ShortName(string fieldname)
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

    #region Static members

    private const string ArrayPrefix = "$array:";

    static public object GeoJsonFeatureValue(string val)
    {
        if (val != null && val.StartsWith(ArrayPrefix))
        {
            return val.Substring(ArrayPrefix.Length).Split(';');
        }
        return val;
    }

    #endregion
}
