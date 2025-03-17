using System.ComponentModel;

namespace E.Standard.WebGIS.CmsSchema;

public class ServiceLayer2 : ServiceLayer
{
    public ServiceLayer2()
        : base() { }

    [ReadOnly(false)]
    public override string Name
    {
        get
        {
            return base.Name;
        }
        set
        {
            base.Name = value;
        }
    }
}
