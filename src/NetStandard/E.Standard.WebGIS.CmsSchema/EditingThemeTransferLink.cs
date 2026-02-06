using E.Standard.CMS.Core.IO.Abstractions;
using E.Standard.CMS.Core.Schema;
using System.ComponentModel;

namespace E.Standard.WebGIS.CmsSchema;

public class EditingThemeTransferLink : Link, IPersistable
{
    [Category("#category_suppress_autovalues")]
    [Bindable(true)]
    [DisplayName("#suppress_autovalues")]
    public bool SuppressAutovalues { get; set; }


    [Category("#category_suppress_validation")]
    [Bindable(true)]
    [DisplayName("#suppress_validation")]
    public bool SuppressValidation { get; set; }

    public override void Load(IStreamDocument stream)
    {
        base.Load(stream);

        this.SuppressAutovalues = (bool)stream.Load("suppress_autovalues", false);
        this.SuppressValidation = (bool)stream.Load("suppress_validation", false);
    }

    public override void Save(IStreamDocument stream)
    {
        base.Save(stream);

        stream.Save("suppress_autovalues", this.SuppressAutovalues);
        stream.Save("suppress_validation", this.SuppressValidation);
    }
}
