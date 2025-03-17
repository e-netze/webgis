using System;

namespace E.Standard.Ticket.LoginService;

public class TokenType
{
    public string Token { get; set; }
    public DateTime Expires { get; set; }
}
