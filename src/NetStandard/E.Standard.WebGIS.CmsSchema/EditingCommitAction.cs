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

    [DisplayName("#timing")]
    public EditCommitActionTiming ActionTiming { get; set; } // before/after

    [DisplayName("#protocol")]
    public EditCommitActionProtocol ActionProtocol { get; set; } // HTTP_GET, HTTP_POST

    [DisplayName("#target")]
    public string ActionTarget { get; set; }  // Url

    [DisplayName("#headers")]
    public string[] ActionHeaders { get; set; } // HTTP Headers (ContentType, Authorization)

    [DisplayName("#payload")]
    public string ActionPayload { get; set; } // content

    [DisplayName("#success_message")]
    public string SuccessMessage { get; set; }

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

        this.ActionTiming = (EditCommitActionTiming)stream.Load(PersistNames.EditingCommitAction.ActionTiming, (int)EditCommitActionTiming.Before_Insert);
        this.ActionProtocol = (EditCommitActionProtocol)stream.Load(PersistNames.EditingCommitAction.ActionProtocol, (int)EditCommitActionProtocol.Http_Get);
        this.ActionTarget = (string)stream.Load(PersistNames.EditingCommitAction.ActionTarget, string.Empty);
        this.ActionPayload = (string)stream.Load(PersistNames.EditingCommitAction.ActionPayload, string.Empty);
        this.SuccessMessage = (string)stream.Load(PersistNames.EditingCommitAction.SuccessMessage, string.Empty);

        string headersJson = (string)stream.Load(PersistNames.EditingCommitAction.ActionHeaders, String.Empty);
        this.ActionHeaders =
            String.IsNullOrWhiteSpace(headersJson)
                ? [""]
                : System.Text.Json.JsonSerializer.Deserialize<string[]>(headersJson);
    }

    public override void Save(IStreamDocument stream)
    {
        base.Save(stream);

        stream.Save(PersistNames.EditingCommitAction.ActionTiming, (int)this.ActionTiming);
        stream.Save(PersistNames.EditingCommitAction.ActionProtocol, (int)this.ActionProtocol);
        stream.Save(PersistNames.EditingCommitAction.ActionTarget, this.ActionTarget ?? String.Empty);
        stream.Save(PersistNames.EditingCommitAction.ActionPayload, this.ActionPayload ?? String.Empty);
        stream.Save(PersistNames.EditingCommitAction.SuccessMessage, this.SuccessMessage ?? String.Empty);

        var headers = this.ActionHeaders?.Where(x => !String.IsNullOrWhiteSpace(x));
        if (headers?.Any() == true)
        {
            stream.Save(PersistNames.EditingCommitAction.ActionHeaders, System.Text.Json.JsonSerializer.Serialize(headers));
        }
    }

    #endregion
}
