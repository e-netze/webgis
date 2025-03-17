using E.Standard.WebMapping.Core.Abstraction;
using System;

namespace E.Standard.WebMapping.Core;

public class Field : IField
{
    private readonly string _name;
    private readonly string _alias;
    private FieldType _type;

    public Field(string name)
    {
        _name = _alias = name;
        _type = FieldType.String;
    }

    public Field(string name, FieldType type)
    {
        _name = _alias = name;
        _type = type;
    }

    public Field(string name, string alias, FieldType type)
    {
        _name = name;
        _alias = String.IsNullOrEmpty(alias) ? name : alias;
        _type = type;
    }

    #region IField Member

    public string Name
    {
        get { return _name; }
    }

    public string Alias
    {
        get { return _alias; }
    }

    public FieldType Type
    {
        get { return _type; }
    }

    #endregion

    public static Type TypeOf(FieldType type)
    {
        switch (type)
        {
            case FieldType.String:
                return typeof(string);
            case FieldType.SmallInteger:
                return typeof(short);
            case FieldType.Shape:
                return typeof(object);
            case FieldType.Interger:
                return typeof(int);
            case FieldType.ID:
                return typeof(int);
            case FieldType.Float:
                return typeof(float);
            case FieldType.Double:
                return typeof(double);
            case FieldType.Date:
                return typeof(DateTime);
            case FieldType.Char:
                return typeof(char);
            case FieldType.Boolean:
                return typeof(bool);
            case FieldType.BigInteger:
                return typeof(long);
        }

        return typeof(object);
    }

    internal void ChangeFieldType(FieldType type)
    {
        _type = type;
    }
}
