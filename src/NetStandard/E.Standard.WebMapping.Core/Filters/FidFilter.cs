using E.Standard.WebMapping.Core.Collections;
using System;
using System.Collections.Generic;
using System.Text;

namespace E.Standard.WebMapping.Core.Filters;

public class FidFilter : QueryFilter
{
    private readonly List<int> _ids = new List<int>();

    private FidFilter(FidFilter filter)
        : base(filter)
    {
        _ids = ListOps<int>.Clone(filter._ids);
        _idFieldName = filter._idFieldName;
    }

    public override string Where
    {
        get
        {
            StringBuilder sb = new StringBuilder();

            sb.Append(_idFieldName + " in (");
            for (int i = 0; i < _ids.Count; i++)
            {
                if (i > 0)
                {
                    sb.Append(",");
                }

                sb.Append(_ids[i].ToString());
            }
            sb.Append(")");

            return sb.ToString();
        }
        set
        {
            throw new NotSupportedException();
        }
    }

    public override QueryFilter Clone()
    {
        return new FidFilter(this);
    }
}
