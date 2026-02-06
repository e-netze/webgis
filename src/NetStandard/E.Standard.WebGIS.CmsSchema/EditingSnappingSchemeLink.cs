using E.Standard.CMS.Core.IO.Abstractions;
using E.Standard.CMS.Core.Schema;
using System;
using System.ComponentModel;

namespace E.Standard.WebGIS.CmsSchema;

public class EditingSnappingSchemeLink : Link
{
    public EditingSnappingSchemeLink()
    {
        this.Nodes = this.Edges = this.EndPoints = true;
    }

    #region Properties

    [DisplayName("#nodes")]
    public bool Nodes { get; set; }
    [DisplayName("#edges")]
    public bool Edges { get; set; }
    [DisplayName("#end_points")]
    public bool EndPoints { get; set; }

    [Category("Topologie")]
    [DisplayName("Fixieren auf:")]
    public string[] FixTo { get; set; }

    #endregion

    #region IPersistable

    public override void Load(IStreamDocument stream)
    {
        base.Load(stream);

        this.Nodes = (bool)stream.Load("nodes", true);
        this.Edges = (bool)stream.Load("edges", true);
        this.EndPoints = (bool)stream.Load("endpoints", true);

        this.FixTo = ((string)stream.Load("fix_to", String.Empty)).Split('|');
    }

    public override void Save(IStreamDocument stream)
    {
        base.Save(stream);

        stream.Save("nodes", this.Nodes);
        stream.Save("edges", this.Edges);
        stream.Save("endpoints", this.EndPoints);

        if (FixTo != null && FixTo.Length > 0)
        {
            stream.Save("fix_to", String.Join("|", FixTo));
        }
    }

    #endregion
}
