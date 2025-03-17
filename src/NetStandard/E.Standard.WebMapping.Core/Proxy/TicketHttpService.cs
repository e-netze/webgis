using E.Standard.Web.Abstractions;
using System;
using System.Threading.Tasks;

namespace E.Standard.WebMapping.Core.Proxy;

public class TicketHttpService
{
    public TicketHttpService(string ticketServiceInstance, string user, string password)
    {
        TicketServiceInstance = ticketServiceInstance;
        Ticket = null;
        User = user;
        Password = password;
        MinimumValidSeconds = 60; // Sekunden vor Ablauf wird neues Ticket geholt
    }

    #region Properties

    public string TicketServiceInstance { get; set; }
    private TicketType Ticket { get; set; }
    private string User { get; set; }
    private string Password { get; set; }
    public int MinimumValidSeconds { get; }

    public string LastErrorMessage { get; private set; }

    #endregion

    public string ModifyUrl(IHttpService httpService, string url)
    {
        if (String.IsNullOrEmpty(TicketServiceInstance))
        {
            return url;
        }

        if (Ticket == null || Ticket.WillExpired(MinimumValidSeconds))
        {
            if (!GetTicket(httpService).GetAwaiter().GetResult())
            {
                throw new Exception(LastErrorMessage);
            }
        }

        #region Remove an exising ticket from Url

        while (url.Contains("&ogc_ticket=") || url.Contains("?ogc_ticket="))
        {
            int pos = url.IndexOf("&ogc_ticket=");
            if (pos < 0)
            {
                pos = url.IndexOf("?ogc_ticket");
            }

            int pos2 = url.IndexOf("&", pos + 1);
            if (pos2 < 0)
            {
                pos2 = url.Length;
            }

            url = url.Substring(0, pos + 1) + url.Substring(pos2, url.Length - pos2);
            while (url.Contains("&&"))
            {
                url = url.Replace("&&", "&");
            }

            url = url.Replace("?&", "?");
        }

        #endregion

        return httpService.AppendParametersToUrl(url, $"ogc_ticket={this.Ticket.Token}");
    }

    #region Helper

    //private readonly object _lockThis = new object();
    async private Task<bool> GetTicket(IHttpService httpService)
    {
        //lock (_lockThis)
        {
            if (Ticket != null && !Ticket.WillExpired(MinimumValidSeconds))
            {
                return true;
            }

            try
            {
                var ticket = await TicketClient.GetTicketType(
                            httpService,
                            TicketServiceInstance,
                            User, Password,
                            httpService.GetProxy(TicketServiceInstance),
                            MinimumValidSeconds);

                this.Ticket = ticket;

                return true;
            }
            catch (Exception ex)
            {
                this.LastErrorMessage = ex.Message;
                return false;
            }
        }
    }

    #endregion
}
