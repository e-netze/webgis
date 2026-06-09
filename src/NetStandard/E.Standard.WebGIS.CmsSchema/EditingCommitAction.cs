using System;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;

using E.Standard.CMS.Core;
using E.Standard.CMS.Core.IO.Abstractions;
using E.Standard.CMS.Core.Schema;
using E.Standard.CMS.Core.Schema.Abstraction;
using E.Standard.CMS.Core.UI.Abstraction;
using E.Standard.CMS.UI.Controls;
using E.Standard.WebGIS.CMS;

namespace E.Standard.WebGIS.CmsSchema;

public class EditingCommitAction : CopyableXml, IUI, ICreatable, IDisplayName
{
    public EditingCommitAction()
    {
        base.StoreUrl = false;
        base.ValidateUrl = false;
    }

    #region Properties

    [DisplayName("Timing")]
    [Description("Gibt an, ob die Action vor oder nach dem Speichen des Objektes passiert")]
    public EditCommitActionTiming ActionTiming { get; set; } // before/after

    [DisplayName("Protocol")]
    [Description("Gibt an, über welches Protokol die Action aufgerufen wird")]
    public EditCommitActionProtocol ActionProtocol { get; set; } // HTTP_GET, HTTP_POST

    [DisplayName("Target")]
    [Description("""
        Gibt das Ziel der Action an. Bei HTTP ist das Ziel die Url der Action.
        Target kann Platzhalter [FELDNAME] angefürht werden, um Werte an das 
        Ziel zu übergeben.
        """)]
        
    public string ActionTarget { get; set; }  // Url

    [DisplayName("Headers")]
    [Description("Hier können Header angegeben werden, die beim HTTP Request beispielsweise zur Authentifizierung oder zur Angabe eines Content-Types verwendet werden können.")]
    public string[] ActionHeaders { get; set; } // HTTP Headers (ContentType, Authorization)

    [DisplayName("Payload")]
    [Description("""
        Die Daten, die an die Action übergeben werden. Bei HTTP_GET können das Url-Parameter sein, 
        bei HTTP_POST ist der Payload der Request Body.
        Im Text können Platzhalter [FELDNAME] angeführt werden, um Werte aus dem aktuellen Feature 
        an das Ziel (Target) zu übergeben.
        """)]
        
    public string ActionPayload { get; set; } // content

    #endregion

    #region IUI Member

    public IUIControl GetUIControl(bool create)
    {
        IInitParameter ip = new NameUrlControl();
        ((NameUrlControl)ip).UrlIsVisible = false;

        ip.InitParameter = this;

        return ip;
    }

    #endregion

    #region ICreatable Member

    override public string CreateAs(bool appendRoot)
    {
        return $"s{GuidEncoder.Encode(Guid.NewGuid())}";
    }

    override public Task<bool> CreatedAsync(string FullName)
    {
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

    [Browsable(false)]
    public override string NodeTitle
    {
        get { return "EditMask Commit Action"; }
    }

    #region IPersistable

    public override void Load(IStreamDocument stream)
    {
        base.Load(stream);

        this.ActionTiming = (EditCommitActionTiming)stream.Load("timing", (int)EditCommitActionTiming.Before_Insert);
        this.ActionProtocol = (EditCommitActionProtocol)stream.Load("protocol", (int)EditCommitActionProtocol.Http_Get);
        this.ActionTarget = (string)stream.Load("target", string.Empty);
        this.ActionPayload = (string)stream.Load("payload", string.Empty);

        string headersJson = (string)stream.Load("headers", String.Empty);
        this.ActionHeaders =
            String.IsNullOrWhiteSpace(headersJson)
                ? [""]
                : System.Text.Json.JsonSerializer.Deserialize<string[]>(headersJson);
    }

    public override void Save(IStreamDocument stream)
    {
        base.Save(stream);

        stream.Save("timing", (int)this.ActionTiming);
        stream.Save("protocol", (int)this.ActionProtocol);
        stream.Save("target", this.ActionTarget ?? String.Empty);
        stream.Save("payload", this.ActionPayload ?? String.Empty);

        var headers = this.ActionHeaders?.Where(x => !String.IsNullOrWhiteSpace(x));
        if (headers?.Any() == true)
        {
            stream.Save("headers", System.Text.Json.JsonSerializer.Serialize(headers));
        }
    }

    #endregion
}
