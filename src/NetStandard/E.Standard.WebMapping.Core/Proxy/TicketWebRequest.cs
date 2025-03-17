using System;
using System.Net;
using System.Text;

namespace E.Standard.WebMapping.Core.Proxy;

public class TicketWebRequest //: IWebRequestWrapper
{
    public TicketWebRequest(string ticketServiceInstance, string user, string password)
    {
        TicketServiceInstance = ticketServiceInstance;
        Ticket = null;
        User = user;
        Password = password;
        MinimumValidSeconds = 3600 * 8;  // 8 hours
    }

    public WebProxy WebProxy { get; set; }
    public string TicketServiceInstance { get; set; }

    public int MinimumValidSeconds { get; set; }

    private TicketType Ticket { get; set; }
    private string User { get; set; }
    private string Password { get; set; }

    #region IWebRequestWrapper Member

    //public HttpWebRequest Create(string url)
    //{
    //    if (String.IsNullOrEmpty(TicketServiceInstance.Trim()))
    //    {
    //        return (HttpWebRequest)HttpWebRequest.Create(url);
    //    }

    //    if (Ticket == null)
    //    {
    //        if (!GetTicket())
    //        {
    //            throw new Exception(LastErrorMessage);
    //        }
    //    }

    //    while (url.Contains("&ogc_ticket=") || url.Contains("?ogc_ticket="))
    //    {
    //        int pos = url.IndexOf("&ogc_ticket=");
    //        if (pos < 0)
    //        {
    //            pos = url.IndexOf("?ogc_ticket");
    //        }

    //        int pos2 = url.IndexOf("&", pos + 1);
    //        if (pos2 < 0)
    //        {
    //            pos2 = url.Length;
    //        }

    //        url = url.Substring(0, pos + 1) + url.Substring(pos2, url.Length - pos2);
    //        while (url.Contains("&&"))
    //        {
    //            url = url.Replace("&&", "&");
    //        }

    //        url = url.Replace("?&", "?");
    //    }

    //    return (HttpWebRequest)HttpWebRequest.Create(url + "&ogc_ticket=" + this.Ticket.Token);
    //}

    public void AppendBasicAuth(HttpWebRequest request, string user, string pwd)
    {
        if (String.IsNullOrEmpty(TicketServiceInstance.Trim()))
        {
            if (!String.IsNullOrEmpty(user.Trim()) && !String.IsNullOrEmpty(pwd.Trim()))
            {
                user = user.Trim();
                pwd = pwd.Trim();
                string authBase64 = Convert.ToBase64String(Encoding.ASCII.GetBytes(user + ":" + pwd));

                string auth = "Basic " + authBase64;
                request.Headers.Add("Authorization", auth);
            }
        }
    }

    public void AppendBasicAuth(HttpWebRequest request, string authBase64)
    {
        if (String.IsNullOrEmpty(TicketServiceInstance.Trim()))
        {
            if (!String.IsNullOrEmpty(authBase64))
            {
                string auth = "Basic " + authBase64;
                request.Headers.Add("Authorization", auth);
            }
        }
    }

    //async public Task<WebResponse> GetResponseAsync(HttpWebRequest request)
    //{
    //    if (String.IsNullOrEmpty(TicketServiceInstance.Trim()))
    //    {
    //        return await request.GetResponseAsync();
    //    }

    //    for (int i = 0; i < 3; i++)
    //    {
    //        try
    //        {
    //            return await request.GetResponseAsync();
    //        }
    //        catch (Exception ex)
    //        {
    //            if (i == 2)
    //            {
    //                LastErrorMessage = ex.Message;
    //                throw;
    //            }

    //            Ticket = null;
    //            GetTicket();

    //            request = this.Create(request.RequestUri.ToString());
    //        }
    //    }

    //    throw new Exception(LastErrorMessage);
    //}

    public string LastErrorMessage
    {
        get;
        set;
    }

    #endregion

    //    private object _lockThis = new object();
    //    private bool GetTicket(IHttpService httpService)
    //    {
    //        lock (_lockThis)
    //        {
    //            if (Ticket != null && !Ticket.WillExpired(MinimumValidSeconds))
    //            {
    //                return true;
    //            }

    //            try
    //            {
    //                var ticket = TicketClient.GetTicketType(
    //                            httpService,
    //                            TicketServiceInstance, 
    //                            User, Password, 
    //                            WebProxy, MinimumValidSeconds);

    //                this.Ticket = ticket;

    //                return true;
    //            }
    //            catch (Exception ex)
    //            {
    //                this.LastErrorMessage = ex.Message;
    //                return false;
    //            }
    //        }
    //    }
}
