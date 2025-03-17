using E.Standard.Api.App.Ogc;
using E.Standard.Custom.Core.Models;
using E.Standard.WebMapping.Core.Abstraction;
using System;
using System.Text;

namespace Api.Core.Models.Ogc;

public class OgcIndexModel
{
    private readonly WmsHelper _wmsHelper;

    public OgcIndexModel(string onlineResource)
    {
        _wmsHelper = new WmsHelper(onlineResource, new int[] { 4326 });
    }

    public IMapService[] WmsServices { get; set; }
    public string LoginService { get; set; }
    public string LogoutService { get; set; }
    public string Username { get; set; }

    public CustomToken CustomToken { get; set; }

    public string Links(string link)
    {
        if (this.CustomToken == null)
        {
            return OnlineResourcePath() + link;
        }

        StringBuilder sb = new StringBuilder();
        if (this.CustomToken.ExpireDate > DateTime.Now)
        {
            sb.Append("<strong>Gültig bis: " + this.CustomToken.ExpireDate.ToLocalTime().ToShortDateString() + " " + this.CustomToken.ExpireDate.ToLocalTime().ToShortTimeString() + "</strong><br/>");
            var l = OnlineResourcePath(false) + link + ServiceParameters(false);
            sb.Append("<a href='" + l + "&REQUEST=GetCapabilities' target=_blank>" + l + "</a>");
            sb.Append("<br/>");
        }
        if (this.CustomToken.ExpireDateLong > DateTime.Now && !String.IsNullOrWhiteSpace(this.CustomToken.LongLivingToken))
        {
            sb.Append("<strong>Gültig bis: " + this.CustomToken.ExpireDateLong.ToLocalTime().ToShortDateString() + " " + this.CustomToken.ExpireDateLong.ToLocalTime().ToShortTimeString() + " nur HTTPS!!</strong><br/>");
            var l = OnlineResourcePath(true) + link + ServiceParameters(true);
            sb.Append("<a href='" + l + "&REQUEST=GetCapabilities' target=_blank>" + l + "</a>");
            sb.Append("<br/>");
        }

        return sb.ToString();
    }

    #region Helper

    private string ServiceParameters(bool useLongLivingToken)
    {
        if (this.CustomToken == null)
        {
            return String.Empty;
        }

        if (useLongLivingToken && !String.IsNullOrEmpty(this.CustomToken.LongLivingToken))
        {
            return "&ogc_ticket=" + this.CustomToken.LongLivingToken;
        }

        return "&ogc_ticket=" + this.CustomToken.Token;
    }

    private string OnlineResourcePath(bool useLongLivingToken = false)
    {
        return _wmsHelper.OnlineResourcePath(useLongLivingToken && this.CustomToken != null && !String.IsNullOrWhiteSpace(this.CustomToken.LongLivingToken));
    }

    #endregion
}