using E.Standard.CMS.Core.IO.Abstractions;
using E.Standard.CMS.Core.Schema;
using E.Standard.CMS.Core.Schema.Abstraction;
using System;
using System.ComponentModel;
using System.Linq;

namespace E.Standard.WebGIS.CmsSchema;

public class GdiTools : SchemaNode, IEditable
{
    private string _verticalAlignmentConfig = String.Empty;

    #region Properties

    [Browsable(true)]
    [DisplayName("#vertical_alignment_config")]
    [Category("#category_vertical_alignment_config")]
    [Editor(typeof(TypeEditor.ProfilesConfigEditor),
        typeof(TypeEditor.ITypeEditor))]
    public string VerticalAlignmentConfig
    {
        get { return _verticalAlignmentConfig; }
        set { _verticalAlignmentConfig = value; }
    }

    [Browsable(true)]
    [DisplayName("Radien [m]")]
    [Category("Umgebungskreis")]
    public int[] MarkerCircleRadii
    {
        get;
        set;
    }

    #endregion

    #region IPersistable Member

    public void Load(IStreamDocument stream)
    {
        _verticalAlignmentConfig = (string)stream.Load("verticalalignmentconfig", String.Empty);

        var radii = (string)stream.Load("markercircleradii", String.Empty);
        if (!String.IsNullOrWhiteSpace(radii))
        {
            this.MarkerCircleRadii = radii.Split(',').Select(r => int.Parse(r)).ToArray();
        }
    }

    public void Save(IStreamDocument stream)
    {
        stream.Save("verticalalignmentconfig", _verticalAlignmentConfig);

        if (this.MarkerCircleRadii != null && this.MarkerCircleRadii.Length > 0)
        {
            stream.Save("markercircleradii", String.Join(",", this.MarkerCircleRadii.OrderBy(r => r).ToArray()));
        }
        else
        {
            stream.Remove("markercircleradii");
        }
    }

    #endregion
}
