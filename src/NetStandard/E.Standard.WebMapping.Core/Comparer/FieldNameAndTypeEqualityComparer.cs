#nullable enable

using E.Standard.WebMapping.Core.Extensions;
using System;
using System.Collections.Generic;

namespace E.Standard.WebMapping.Core.Comparer;

public class FieldNameAndTypeEqualityComparer : IEqualityComparer<Abstraction.IField>
{
    public bool Equals(Abstraction.IField? x, Abstraction.IField? y)
    {
        return x.HasEqualNameAndType(y);
    }
    public int GetHashCode(Abstraction.IField obj)
    {
        return 0;
    }
}
