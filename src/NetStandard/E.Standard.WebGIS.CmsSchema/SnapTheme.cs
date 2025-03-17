using E.Standard.CMS.Core;
using E.Standard.CMS.Core.IO;
using E.Standard.CMS.Core.IO.Abstractions;
using E.Standard.CMS.Core.Schema;
using E.Standard.CMS.Core.Schema.Abstraction;
using E.Standard.CMS.Core.UI.Abstraction;
using E.Standard.WebGIS.CmsSchema.UI;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;

namespace E.Standard.WebGIS.CmsSchema;

public class SnapTheme : Link, IPersistable, IEditable
{
    private bool _vertex = true, _edge = true, _startend_point = true;

    #region Properties
    /*
    [DisplayName("St�tzpunkt (Vertex)")]
    [Category("Snappen auf...")]
    public bool Vertex
    {
        get { return _vertex; }
        set { _vertex = value; }
    }
    [DisplayName("Kante (Linie)")]
    [Category("Snappen auf...")]
    public bool Edge
    {
        get { return _edge; }
        set { _edge = value; }
    }
    [DisplayName("Start/Endpunkt")]
    [Category("Snappen auf...")]
    public bool StartEndPoint
    {
        get { return _startend_point; }
        set { _startend_point = value; }
    }
     * */
    #endregion

    #region IPersistable Member

    public override void Load(IStreamDocument stream)
    {
        base.Load(stream);

        _vertex = (bool)stream.Load("vertex", true);
        _edge = (bool)stream.Load("edge", true);
        _startend_point = (bool)stream.Load("startendpoint", true);
    }

    public override void Save(IStreamDocument stream)
    {
        base.Save(stream);

        stream.Save("vertex", _vertex);
        stream.Save("edge", _edge);
        stream.Save("startendpoint", _startend_point);
    }

    #endregion
}

public class SnapSchema : NameUrl, IUI, ICreatable, IDisplayName, IEditable
{
    private int _minScale = 3000;

    private readonly CmsItemTransistantInjectionServicePack _servicePack;

    public SnapSchema(CmsItemTransistantInjectionServicePack servicePack)
    {
        _servicePack = servicePack;

        this.ValidateUrl = false;
        this.StoreUrl = false;
    }

    #region Properties
    [Browsable(true)]
    [DisplayName("Snappen ab einem Ma�stab von 1:")]
    public int MinScale
    {
        get { return _minScale; }
        set { _minScale = value; }
    }
    #endregion

    [Browsable(false)]
    public string[] SnapThemeIds { get; set; }

    #region IUI Member

    public IUIControl GetUIControl(bool create)
    {
        IInitParameter ip = new NewSnappingControl(_servicePack, this);
        ip.InitParameter = this;

        return ip;
    }

    #endregion

    #region ICreatable Member

    public string CreateAs(bool appendRoot)
    {
        if (appendRoot)
        {
            return (string.IsNullOrWhiteSpace(this.Url) ? Crypto.GetID() : this.Url) + @"\.general";
        }
        else
        {
            return ".general";
        }
    }

    public Task<bool> CreatedAsync(string FullName)
    {
        if (SnapThemeIds != null)
        {
            foreach (var layerId in SnapThemeIds)
            {
                var serviceLayer = this.CmsManager.SchemaNodeInstances(_servicePack, Helper.TrimPathRight(this.RelativePath, 3) + "/Themes", true)
                        .Where(o => o is ServiceLayer && ((ServiceLayer)o).Id == layerId)
                        .FirstOrDefault() as ServiceLayer;

                if (serviceLayer != null)  // Link to SnapTheme
                {
                    var link = new Link(serviceLayer.RelativePath);

                    string newLinkName = Helper.NewLinkName();
                    IStreamDocument xmlStream = DocumentFactory.New(this.CmsManager.ConnectionString);
                    link.Save(xmlStream);
                    xmlStream.SaveDocument(this.CmsManager.ConnectionString + "/" + Helper.TrimPathRight(this.RelativePath, 1) + @"/" + newLinkName);
                }
            }
        }

        return Task<bool>.FromResult(true);
    }

    #endregion

    #region IDisplayName Member

    [Browsable(false)]
    public string DisplayName
    {
        get { return this.Name; }
    }

    #endregion

    public override void Load(IStreamDocument stream)
    {
        base.Load(stream);

        _minScale = (int)stream.Load("minscale", 3000);
    }

    public override void Save(IStreamDocument stream)
    {
        base.Save(stream);

        stream.Save("minscale", _minScale);
    }
}

public class SnapSchemaLink : Link
{
}
