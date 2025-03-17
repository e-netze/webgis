using E.Standard.CMS.Core.IO.Abstractions;
using E.Standard.CMS.Core.Schema;
using System.ComponentModel;

namespace E.Standard.WebGIS.CmsSchema;

public class EditingThemeTransferLink : Link, IPersistable
{
    [Category("Transfer Pipeline")]
    [Bindable(true)]
    [DisplayName("Autovalues ausblenden")]
    [Description("Autovalues werden beim Transfer nicht für die Zielobjekte neu ermittelt. Bestehende Felder werden 1:1 übernommen.")]
    public bool SuppressAutovalues { get; set; }


    [Category("Transfer Pipeline")]
    [Bindable(true)]
    [DisplayName("Validierung ausblenden")]
    [Description("Beim Transfer werden für die Zielobjekte keine Editfeld Validierungen durchgeführt. Bestehende Felder werden 1:1 übernommen.")]
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
