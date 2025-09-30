using E.Standard.ThreadSafe;
using E.Standard.WebMapping.Core.Abstraction;
using System;
using System.Text;

namespace E.Standard.WebMapping.Core.Collections;

public class FieldCollection : ThreadSafeList<IField>
{
    public IField FindField(string name, StringComparison stringComparison = StringComparison.InvariantCulture)
    {
        foreach (IField field in this)
        {
            if (field?.Name == null)
            {
                continue;
            }

            if (field.Name.Equals(name, stringComparison))
            {
                return field;
            }
        }
        foreach (IField field in this)
        {
            if (field?.Name == null)
            {
                continue;
            }

            if (Attribute.ShortName(field.Name).Equals(name, stringComparison))
            {
                return field;
            }
        }
        return null;
    }

    public string ToString(string seperator)
    {
        StringBuilder sb = new StringBuilder();

        foreach (IField field in this)
        {
            if (field == null)
            {
                continue;
            }

            if (sb.Length > 0)
            {
                sb.Append(seperator);
            }

            sb.Append(field.Name);
        }

        return sb.ToString();
    }
}

