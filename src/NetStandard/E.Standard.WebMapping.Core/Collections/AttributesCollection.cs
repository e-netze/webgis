using E.Standard.ThreadSafe;

namespace E.Standard.WebMapping.Core.Collections;

public class AttributesCollection : ThreadSafeList<Attribute>
{
    public Attribute this[string name]
    {
        get
        {
            foreach (Attribute attribute in this)
            {
                if (attribute.Name == name)
                {
                    return attribute;
                }
            }
            foreach (Attribute attribute in this)
            {
                if (Attribute.ShortName(attribute.Name) == name)
                {
                    return attribute;
                }
            }

            // Bei Kunden stimmen zB die Groß/Kleinschreibungen beim den Schema-/Tabellennamen nicht zusammen
            // Workaround wäre schlussendlich auch noch nur die Kurznamen zu vergleichen...
            string shortName = Attribute.ShortName(name);
            foreach (Attribute attribute in this)
            {
                if (Attribute.ShortName(attribute.Name) == shortName)
                {
                    return attribute;
                }
            }
            return null;
        }
    }

    public Attribute GetCaseInsensitiv(string name)
    {
        name = name.ToLower();
        foreach (Attribute attribute in this)
        {
            if (attribute.Name.ToLower() == name)
            {
                return attribute;
            }
        }
        foreach (Attribute attribute in this)
        {
            if (Attribute.ShortName(attribute.Name.ToLower()) == name)
            {
                return attribute;
            }
        }

        // Bei Kunden stimmen zB die Groß/Kleinschreibungen beim den Schema-/Tabellennamen nicht zusammen
        // Workaround wäre schlussendlich auch noch nur die Kurznamen zu vergleichen...
        string shortName = Attribute.ShortName(name);
        foreach (Attribute attribute in this)
        {
            if (Attribute.ShortName(attribute.Name.ToLower()) == shortName)
            {
                return attribute;
            }
        }
        return null;
    }

    public bool Exists(string name)
    {
        return this[name] != null;
    }

    public bool CaseInsensitiv(string name)
    {
        return this.GetCaseInsensitiv(name) != null;
    }

    public void SetOrAdd(string name, string value)
    {
        var attribute = this[name];

        if (attribute != null)
        {
            attribute.Value = value;
        }
        else
        {
            this.Add(new Attribute(name, value));
        }
    }

    public void SetOrAddCaseInsensitiv(string name, string value)
    {
        var attribute = this.GetCaseInsensitiv(name);

        if (attribute != null)
        {
            attribute.Value = value;
        }
        else
        {
            this.Add(new Attribute(name, value));
        }
    }
}
