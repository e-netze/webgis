#nullable enable

using System;
using System.Collections.Generic;

namespace E.Standard.WebMapping.Core.Extensions;

static public class FieldExtensions
{
    static public bool HasEqualNameAndType(this Abstraction.IField? field, Abstraction.IField? otherField)
    {
        if (field == null || otherField == null)
        {
            return false;
        }

        return field.Name == otherField.Name && field.Type == otherField.Type;
    }
}
