using E.Standard.WebMapping.Core.Abstraction;
using System;
using System.Collections.Generic;

namespace E.Standard.WebMapping.Core;


// Domain: Bisher noch nicht verwendet!

abstract public class FieldDomain
{
    abstract public string GetValue(object code);
}

public class CodedValueFieldDomain : FieldDomain
{
    private Dictionary<object, string> _values;

    public void AddCodedValue(object code, string val)
    {
        if (_values == null)
        {
            _values = new Dictionary<object, string>();
        }

        _values.Add(code, val);
    }

    public override string GetValue(object code)
    {
        try
        {
            return _values[code];
        }
        catch
        {
            return code != null ? code.ToString() : String.Empty;
        }
    }
}

public class DomainField : Field, IDomainField
{
    private FieldDomain _domain;

    public DomainField(string name, FieldDomain domain)
        : this(name, FieldType.String, domain)
    {
    }
    public DomainField(string name, FieldType type, FieldDomain domain)
        : base(name, type)
    {
        _domain = domain;
    }

    public FieldDomain Domain
    {
        get { return _domain; }
    }
}
