using System;
using System.Linq;

namespace E.Standard.WebMapping.Core.Api.Bridge;

public class ApiOidsFilter : ApiQueryFilter
{
    public ApiOidsFilter(long[] oids)
        : base()
    {
        this.Oids = oids;
        QueryItems.Add("#oids#", String.Join(",", oids));
    }

    public ApiOidsFilter(int[] oids)
        : this(oids.Select(id => (long)id).ToArray())
    {

    }

    public long[] Oids { get; set; }
}
