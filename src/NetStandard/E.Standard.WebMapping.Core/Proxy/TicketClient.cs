using E.Standard.Web.Abstractions;
using System;
using System.Net;
using System.Threading.Tasks;

namespace E.Standard.WebMapping.Core.Proxy;

public class TicketClient
{
    public TicketClient(string url)
    {
        if (!url.ToLower().EndsWith("ticket/login.asmx"))
        {
            url += "/ticket/login.asmx";
        }

        Url = url;
    }

    public string Url { get; set; }

    public string Login(string user, string password, WebProxy webProxy = null)
    {
        throw new NotImplementedException();
    }

    public bool Logout(string ticket)
    {
        throw new NotImplementedException();
    }

    async public static Task<TicketType> GetTicketType(
            IHttpService http,
            string url, string user,
            string password,
            WebProxy webProxy = null,
            int minimunSecondsValid = 3600)
    {
        if (url.ToLower().EndsWith(".asmx"))
        {
            throw new NotImplementedException("WebGIS .asmx login not supported. Use krako url in WebGIS CMS!");
        }
        else
        {
            var login = new Ticket.LoginService.Login(url);
            var tokenType = await login.GetTokenType(http, user, password, webProxy, minimunSecondsValid);

            return new TicketType()
            {
                Username = user,
                Token = tokenType.Token,
                Expires = tokenType.Expires
            };
        }
    }
}
