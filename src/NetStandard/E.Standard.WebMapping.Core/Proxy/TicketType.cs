using System;

namespace E.Standard.WebMapping.Core.Proxy;

public class TicketType
{
    public string Username { get; set; }
    public string Token { get; set; }
    public DateTime Expires { get; set; }

    public bool WillExpired(int inSeconds = 0)
    {
        if (DateTime.UtcNow.AddSeconds(inSeconds) > this.Expires)
        {
            return true;
        }

        return false;
    }

    public bool IsExpired
    {
        get
        {
            return WillExpired();
        }
    }
}
